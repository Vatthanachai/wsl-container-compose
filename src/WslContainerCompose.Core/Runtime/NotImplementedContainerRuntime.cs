namespace WslContainerCompose.Core.Runtime;

/// <summary>
/// Placeholder <see cref="IContainerRuntime"/> used until the real adapter over
/// `Microsoft.WSL.Containers` (Session/Container) is written - see Progress.md "Next up".
/// </summary>
public sealed class NotImplementedContainerRuntime : IContainerRuntime
{
    private static NotImplementedException NotYet([System.Runtime.CompilerServices.CallerMemberName] string member = "")
        => new($"{member} is not implemented yet - the real Microsoft.WSL.Containers adapter hasn't been written. See obsidian/wsl-container-compose/Progress.md.");

    public Task<string> CreateSessionAsync(string projectName, CancellationToken cancellationToken = default) => throw NotYet();

    public Task PullImageAsync(string sessionId, string image, CancellationToken cancellationToken = default) => throw NotYet();

    public Task<string> CreateContainerAsync(string sessionId, ContainerSpec spec, CancellationToken cancellationToken = default) => throw NotYet();

    public Task StartContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default) => throw NotYet();

    public Task StopContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default) => throw NotYet();

    public Task DeleteContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default) => throw NotYet();

    public Task<IReadOnlyList<string>> GetContainerLogsAsync(string sessionId, string containerId, CancellationToken cancellationToken = default) => throw NotYet();

    public Task<ContainerStatus> GetContainerStatusAsync(string sessionId, string containerId, CancellationToken cancellationToken = default) => throw NotYet();

    public Task<string?> GetContainerIpAddressAsync(string sessionId, string containerId, CancellationToken cancellationToken = default) => throw NotYet();

    public Task WriteHostsEntriesAsync(string sessionId, string containerId, IReadOnlyDictionary<string, string> hostnameToIp, CancellationToken cancellationToken = default) => throw NotYet();

    public Task TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default) => throw NotYet();
}
