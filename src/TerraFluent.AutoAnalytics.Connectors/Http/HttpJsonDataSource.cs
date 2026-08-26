using System.Globalization;
using System.Text;
using System.Text.Json;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Data.Connections;
using TerraFluent.AutoAnalytics.Data.Sources;

namespace TerraFluent.AutoAnalytics.Connectors.Http;

/// <summary>
/// Reads a JSON array from an HTTP endpoint and normalises it into a <see cref="Dataset"/>.
/// </summary>
/// <remarks>
/// <para>Pull-on-demand only: every call fetches fresh and the rows are discarded once the analysis
/// returns. Nothing is cached, so there is no stale data and no stored copy of the source.</para>
/// <para>The URL comes from server-side configuration, never from the caller, so this cannot be
/// pointed at an arbitrary host by a client. Responses are bounded by a byte cap and a row cap so a
/// large or hostile endpoint cannot exhaust memory.</para>
/// </remarks>
public sealed class HttpJsonDataSource : IAsyncDataSource
{
    /// <summary>Rows read when the connection does not specify its own cap.</summary>
    public const int DefaultMaxRows = 100_000;

    /// <summary>Seconds allowed for the fetch when the connection does not specify its own timeout.</summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>Hard ceiling on the response body, independent of the row cap.</summary>
    private const long MaxResponseBytes = 64L * 1024 * 1024;

    private readonly HttpClient _client;
    private readonly ConnectionDefinition _connection;

    public HttpJsonDataSource(HttpClient client, ConnectionDefinition connection)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Dataset> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_connection.Target, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            // The target is operator-supplied, so this is a configuration error — and the message
            // must still not echo the value.
            throw ConnectionException.Failed(_connection.Name,
                "its configured target is not an absolute http or https URL.");
        }

        int timeout = _connection.TimeoutSeconds is > 0 ? _connection.TimeoutSeconds.Value : DefaultTimeoutSeconds;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeout));

        string payload;
        try
        {
            payload = await FetchAsync(uri, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw ConnectionException.Failed(_connection.Name, $"it did not respond within {timeout}s.");
        }
        catch (HttpRequestException ex)
        {
            // HttpRequestException messages routinely embed the URL, so the reason is described
            // rather than quoted, and the inner exception is redacted before being attached.
            throw ConnectionException.Failed(_connection.Name, "the endpoint could not be reached.", Redact(ex));
        }

        string json = ExtractArray(payload);

        Dataset dataset;
        try
        {
            dataset = new JsonDataSource(json, _connection.Name).Load();
        }
        catch (JsonException ex)
        {
            throw ConnectionException.Failed(_connection.Name,
                "the response was not a JSON array of flat objects.", Redact(ex));
        }

        int maxRows = _connection.MaxRows is > 0 ? _connection.MaxRows.Value : DefaultMaxRows;
        return dataset.RowCount > maxRows ? Truncate(dataset, maxRows) : dataset;
    }

    /// <summary>
    /// Strips the connection's secrets out of an underlying exception before it is attached as an
    /// inner exception.
    /// </summary>
    /// <remarks>
    /// HTTP client exceptions quote the request URL, which for many APIs carries the token in its
    /// query string. That text would then reach anything that stringifies the exception — including
    /// a server log shipped off to an aggregator. Redacting keeps the diagnostic value (the failure
    /// type and shape) while removing the credential, so no caller has to remember not to log it.
    /// </remarks>
    private Exception Redact(Exception ex)
    {
        string message = ex.Message;

        message = Mask(message, _connection.Target);
        if (Uri.TryCreate(_connection.Target, UriKind.Absolute, out var uri))
        {
            message = Mask(message, uri.GetLeftPart(UriPartial.Path));
            message = Mask(message, uri.Host);
        }
        foreach (var value in _connection.Settings.Values)
            message = Mask(message, value);

        // The type name is preserved in the text so the failure is still diagnosable.
        return new InvalidOperationException($"{ex.GetType().Name}: {message}");
    }

    private static string Mask(string text, string secret) =>
        string.IsNullOrEmpty(secret) ? text : text.Replace(secret, "[redacted]", StringComparison.OrdinalIgnoreCase);

    // ── Fetch ─────────────────────────────────────────────────────────────────

    private async Task<string> FetchAsync(Uri uri, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        // Per-connection headers (auth tokens, API keys) come from configuration only.
        foreach (var (key, value) in _connection.Settings)
        {
            const string prefix = "header.";
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                request.Headers.TryAddWithoutValidation(key[prefix.Length..], value);
        }

        using var response = await _client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw ConnectionException.Failed(_connection.Name,
                $"the endpoint returned HTTP {(int)response.StatusCode}.");

        if (response.Content.Headers.ContentLength is long declared && declared > MaxResponseBytes)
            throw ConnectionException.Failed(_connection.Name,
                $"the response is larger than the {MaxResponseBytes / (1024 * 1024)}MB limit.");

        return await ReadBoundedAsync(response, ct).ConfigureAwait(false);
    }

    // Streams the body with a hard byte ceiling — a missing or lying Content-Length must not let an
    // endpoint stream until the process runs out of memory.
    private async Task<string> ReadBoundedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var buffer = new byte[81920];
        using var accumulated = new MemoryStream();

        int read;
        while ((read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            if (accumulated.Length + read > MaxResponseBytes)
                throw ConnectionException.Failed(_connection.Name,
                    $"the response exceeded the {MaxResponseBytes / (1024 * 1024)}MB limit.");
            accumulated.Write(buffer, 0, read);
        }

        return Encoding.UTF8.GetString(accumulated.ToArray());
    }

    // ── Shaping ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the JSON array to analyse. Most APIs wrap their rows in an envelope, so a
    /// <c>jsonPath</c> setting (e.g. <c>data.items</c>) selects the array within it.
    /// </summary>
    private string ExtractArray(string payload)
    {
        _connection.Settings.TryGetValue("jsonPath", out var path);
        if (string.IsNullOrWhiteSpace(path)) return payload;

        try
        {
            using var document = JsonDocument.Parse(payload);
            var element = document.RootElement;

            foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(segment, out var next))
                    throw ConnectionException.Failed(_connection.Name,
                        $"the response has no '{path}' property to read rows from.");
                element = next;
            }

            if (element.ValueKind != JsonValueKind.Array)
                throw ConnectionException.Failed(_connection.Name,
                    $"'{path}' is not a JSON array.");

            return element.GetRawText();
        }
        catch (JsonException ex)
        {
            throw ConnectionException.Failed(_connection.Name, "the response was not valid JSON.", Redact(ex));
        }
    }

    // Keeps the first N rows. Applied after parsing so the cap is enforced on what analysis sees,
    // regardless of how the endpoint paginated.
    private static Dataset Truncate(Dataset dataset, int maxRows)
    {
        var names = dataset.Columns.Select(c => c.Name).ToList();
        var rows = new List<object?[]>(maxRows);

        for (int r = 0; r < maxRows; r++)
        {
            var row = new object?[names.Count];
            for (int c = 0; c < names.Count; c++)
            {
                var values = dataset.Columns[c].Values;
                row[c] = r < values.Count ? values[r] : null;
            }
            rows.Add(row);
        }

        return Dataset.FromRows(dataset.Name, names, rows);
    }
}
