namespace WslContainerCompose.Core.State;

/// <summary>
/// Tracks which session/containers belong to a compose project across separate CLI invocations,
/// since the WSL container API doesn't document a way to enumerate/reconnect to them itself
/// (see Plan.md "State tracking").
/// </summary>
public sealed class ProjectState
{
    public required string ProjectName { get; init; }
    public required string SessionId { get; init; }
    public required IReadOnlyDictionary<string, string> ContainerIdsByService { get; init; }
}
