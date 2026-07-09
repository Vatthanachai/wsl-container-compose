namespace WslContainerCompose.Core.Runtime;

/// <summary>
/// Abstraction over the WSL container feature (`Microsoft.WSL.Containers`: Session -> Container -> Process),
/// so orchestration logic in <see cref="Orchestration.ComposeProject"/> can be unit-tested without real WSL.
/// See Plan.md ("Testability") for why this exists.
/// </summary>
public interface IContainerRuntime
{
    Task<string> CreateSessionAsync(string projectName, CancellationToken cancellationToken = default);

    Task PullImageAsync(string sessionId, string image, CancellationToken cancellationToken = default);

    Task<string> CreateContainerAsync(string sessionId, ContainerSpec spec, CancellationToken cancellationToken = default);

    Task StartContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default);

    Task StopContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default);

    Task DeleteContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetContainerLogsAsync(string sessionId, string containerId, CancellationToken cancellationToken = default);

    Task<ContainerStatus> GetContainerStatusAsync(string sessionId, string containerId, CancellationToken cancellationToken = default);

    Task TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
