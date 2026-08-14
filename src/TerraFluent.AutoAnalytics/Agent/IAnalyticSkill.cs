namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>
/// A deterministic, self-contained unit of analysis the <see cref="AnalyticAgent"/> can invoke.
/// Skills are stateless and thread-safe; they read from the shared <see cref="AgentContext"/> and
/// return a <see cref="SkillResult"/>. Implement this interface to extend the agent with new
/// capabilities (e.g. forecasting, root-cause, segmentation).
/// </summary>
public interface IAnalyticSkill
{
    /// <summary>Stable identifier used in the investigation trace and for de-duplication.</summary>
    string Name { get; }

    /// <summary>Short description of what the skill does.</summary>
    string Description { get; }

    /// <summary>
    /// Returns <see langword="true"/> when this skill can contribute to the given goal and there is
    /// relevant evidence in the context. The agent only executes skills that can handle a goal.
    /// </summary>
    bool CanHandle(AnalyticGoal goal, AgentContext context);

    /// <summary>Runs the skill against the context's current <see cref="AgentContext.Goal"/>.</summary>
    SkillResult Execute(AgentContext context);
}
