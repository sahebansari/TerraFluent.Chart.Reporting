using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TerraFluent.AutoAnalytics.Data.Sources;

/// <summary>
/// Adapter over any <see cref="IEnumerable{T}"/> of POCOs. Public readable instance properties
/// (and, failing that, fields) become columns via reflection.
/// </summary>
public sealed class EnumerableDataSource<T> : IDataSource
{
    private readonly IEnumerable<T> _items;
    private readonly string _name;

    public EnumerableDataSource(IEnumerable<T> items, string? name = null)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _name  = name ?? typeof(T).Name;
    }

    public Dataset Load()
    {
        var materialised = _items.ToList();

        // For anonymous/dynamic scenarios T may be object; infer members from the first item.
        Type elementType = typeof(T);
        if (elementType == typeof(object) && materialised.Count > 0 && materialised[0] is not null)
            elementType = materialised[0]!.GetType();

        var props = elementType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToList();

        var members = props.Select(p => (Name: p.Name, Getter: (Func<object?, object?>)(o => o is null ? null : p.GetValue(o))))
            .ToList();

        if (members.Count == 0)
        {
            var fields = elementType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            members = fields.Select(f => (Name: f.Name, Getter: (Func<object?, object?>)(o => o is null ? null : f.GetValue(o))))
                .ToList();
        }

        var columnNames = members.Select(m => m.Name).ToList();
        var rows = new List<object?[]>(materialised.Count);
        foreach (var item in materialised)
        {
            object? boxed = item;
            var row = new object?[members.Count];
            for (int c = 0; c < members.Count; c++)
            {
                var v = members[c].Getter(boxed);
                row[c] = v is null || v == DBNull.Value ? null : v;
            }
            rows.Add(row);
        }

        return Dataset.FromRows(_name, columnNames, rows);
    }
}
