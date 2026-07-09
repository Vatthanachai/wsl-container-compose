using WslContainerCompose.Core.Runtime;

namespace WslContainerCompose.Core.Tests.Orchestration;

internal sealed class FakeContainerRuntime : IContainerRuntime
{
    private int _nextContainerId;

    public HashSet<string> ImagesThatFailToPull { get; } = [];
    public List<string> StartedContainerIds { get; } = [];
    public List<string> StoppedContainerIds { get; } = [];
    public List<string> DeletedContainerIds { get; } = [];
    public bool SessionTerminated { get; private set; }

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
        => Task.FromResult($"container-{++_nextContainerId}");

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
}
