using WslContainerCompose.Core.Runtime;

namespace WslContainerCompose.Core.Tests.Orchestration;

internal sealed class FakeContainerRuntime : IContainerRuntime
{
    private int _nextContainerId;
    private readonly Dictionary<string, string> _ipsByContainerId = [];

    public HashSet<string> ImagesThatFailToPull { get; } = [];
    public List<string> StartedContainerIds { get; } = [];
    public List<string> StoppedContainerIds { get; } = [];
    public List<string> DeletedContainerIds { get; } = [];
    public bool SessionTerminated { get; private set; }

    public HashSet<string> ContainerIdsThatFailIpLookup { get; } = [];
    public HashSet<string> ContainerIdsThatFailHostsWrite { get; } = [];
    public Dictionary<string, IReadOnlyDictionary<string, string>> HostsEntriesByContainerId { get; } = [];

    public Task<string> CreateSessionAsync(string projectName, CancellationToken cancellationToken = default)
        => Task.FromResult($"session-{projectName}");

    public Task PullImageAsync(string sessionId, string image, CancellationToken cancellationToken = default)
    {
        if (ImagesThatFailToPull.Contains(image))
        {
            throw new InvalidOperationException($"failed to pull '{image}'");
        }

        return Task.CompletedTask;
    }

    public Task<string> CreateContainerAsync(string sessionId, ContainerSpec spec, CancellationToken cancellationToken = default)
    {
        var containerId = $"container-{++_nextContainerId}";
        _ipsByContainerId[containerId] = $"10.0.0.{_nextContainerId}";
        return Task.FromResult(containerId);
    }

    public Task StartContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default)
    {
        StartedContainerIds.Add(containerId);
        return Task.CompletedTask;
    }

    public Task StopContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default)
    {
        StoppedContainerIds.Add(containerId);
        return Task.CompletedTask;
    }

    public Task DeleteContainerAsync(string sessionId, string containerId, CancellationToken cancellationToken = default)
    {
        DeletedContainerIds.Add(containerId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetContainerLogsAsync(string sessionId, string containerId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(["log line 1"]);

    public Task<ContainerStatus> GetContainerStatusAsync(string sessionId, string containerId, CancellationToken cancellationToken = default)
        => Task.FromResult(new ContainerStatus(containerId, containerId, IsRunning: true));

    public Task TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        SessionTerminated = true;
        return Task.CompletedTask;
    }

    public Task<string?> GetContainerIpAddressAsync(string sessionId, string containerId, CancellationToken cancellationToken = default)
    {
        if (ContainerIdsThatFailIpLookup.Contains(containerId))
        {
            throw new InvalidOperationException($"failed to inspect '{containerId}'");
        }

        return Task.FromResult(_ipsByContainerId.GetValueOrDefault(containerId));
    }

    public Task WriteHostsEntriesAsync(string sessionId, string containerId, IReadOnlyDictionary<string, string> hostnameToIp, CancellationToken cancellationToken = default)
    {
        if (ContainerIdsThatFailHostsWrite.Contains(containerId))
        {
            throw new InvalidOperationException($"failed to write hosts entries for '{containerId}'");
        }

        HostsEntriesByContainerId[containerId] = hostnameToIp;
        return Task.CompletedTask;
    }
}
