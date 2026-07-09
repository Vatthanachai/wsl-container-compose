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

    /// <summary>
    /// Returns the container's current IP address, or null if it can't be determined.
    /// Used to wire up service-name discovery via generated `/etc/hosts` entries -
    /// see Plan.md "Networks (provisional)".
    /// </summary>
    Task<string?> GetContainerIpAddressAsync(string sessionId, string containerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes `/etc/hosts` entries into the container mapping each given service name to its IP,
    /// replacing any entries from a previous call. See Plan.md "Networks (provisional)".
    /// </summary>
    Task WriteHostsEntriesAsync(string sessionId, string containerId, IReadOnlyDictionary<string, string> hostnameToIp, CancellationToken cancellationToken = default);

    Task TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
