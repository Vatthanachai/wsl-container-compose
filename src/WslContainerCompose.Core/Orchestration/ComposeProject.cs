using WslContainerCompose.Core.Compose;
using WslContainerCompose.Core.Runtime;
using WslContainerCompose.Core.State;

namespace WslContainerCompose.Core.Orchestration;

public sealed record UpResult(
    IReadOnlyDictionary<string, string> ContainerIdsByService,
    IReadOnlyList<(string Service, Exception Error)> Failures);

/// <summary>
/// Drives a single compose project's lifecycle against an <see cref="IContainerRuntime"/>,
/// persisting progress to a <see cref="ProjectStateStore"/> as it goes.
/// </summary>
public sealed class ComposeProject(ComposeFile composeFile, IContainerRuntime runtime, ProjectStateStore stateStore)
{
    public async Task<UpResult> UpAsync(CancellationToken cancellationToken = default)
    {
        var existingState = await stateStore.LoadAsync(composeFile.ProjectName, cancellationToken);
        var sessionId = existingState?.SessionId
            ?? await runtime.CreateSessionAsync(composeFile.ProjectName, cancellationToken);

        var containerIds = existingState is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(existingState.ContainerIdsByService);

        var failures = new List<(string Service, Exception Error)>();

        foreach (var serviceName in DependencyOrder.ResolveStartOrder(composeFile.Services))
        {
            if (containerIds.ContainsKey(serviceName))
            {
                continue;
            }

            var service = composeFile.Services[serviceName];
            try
            {
                await runtime.PullImageAsync(sessionId, service.Image, cancellationToken);

                var spec = new ContainerSpec(
                    Name: $"{composeFile.ProjectName}-{serviceName}",
                    Image: service.Image,
                    Command: service.Command,
                    Environment: service.Environment,
                    Ports: service.Ports,
                    Volumes: service.Volumes);

                var containerId = await runtime.CreateContainerAsync(sessionId, spec, cancellationToken);
                await runtime.StartContainerAsync(sessionId, containerId, cancellationToken);
                containerIds[serviceName] = containerId;
            }
            catch (Exception ex)
            {
                // Leave already-started containers running and keep going - matches docker
                // compose's default `up` behavior. See Plan.md "Failure handling".
                failures.Add((serviceName, ex));
            }
            finally
            {
                await stateStore.SaveAsync(
                    new ProjectState
                    {
                        ProjectName = composeFile.ProjectName,
                        SessionId = sessionId,
                        ContainerIdsByService = containerIds,
                    },
                    cancellationToken);
            }
        }

        return new UpResult(containerIds, failures);
    }

    public async Task DownAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(composeFile.ProjectName, cancellationToken);
        if (state is null)
        {
            return;
        }

        foreach (var serviceName in DependencyOrder.ResolveStopOrder(composeFile.Services))
        {
            if (!state.ContainerIdsByService.TryGetValue(serviceName, out var containerId))
            {
                continue;
            }

            await runtime.StopContainerAsync(state.SessionId, containerId, cancellationToken);
            await runtime.DeleteContainerAsync(state.SessionId, containerId, cancellationToken);
        }

        await runtime.TerminateSessionAsync(state.SessionId, cancellationToken);
        stateStore.Delete(composeFile.ProjectName);
    }

    public async Task<IReadOnlyList<ContainerStatus>> PsAsync(CancellationToken cancellationToken = default)
    {
        var state = await stateStore.LoadAsync(composeFile.ProjectName, cancellationToken);
        if (state is null)
        {
            return [];
        }

        var statuses = new List<ContainerStatus>();
        foreach (var containerId in state.ContainerIdsByService.Values)
        {
            statuses.Add(await runtime.GetContainerStatusAsync(state.SessionId, containerId, cancellationToken));
        }

        return statuses;
    }

    public async Task<IReadOnlyList<string>> LogsAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var state = await RequireStateAsync(cancellationToken);
        var containerId = RequireContainerId(state, serviceName);
        return await runtime.GetContainerLogsAsync(state.SessionId, containerId, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var state = await RequireStateAsync(cancellationToken);
        foreach (var serviceName in DependencyOrder.ResolveStopOrder(composeFile.Services))
        {
            if (state.ContainerIdsByService.TryGetValue(serviceName, out var containerId))
            {
                await runtime.StopContainerAsync(state.SessionId, containerId, cancellationToken);
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var state = await RequireStateAsync(cancellationToken);
        foreach (var serviceName in DependencyOrder.ResolveStartOrder(composeFile.Services))
        {
            if (state.ContainerIdsByService.TryGetValue(serviceName, out var containerId))
            {
                await runtime.StartContainerAsync(state.SessionId, containerId, cancellationToken);
            }
        }
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    private async Task<ProjectState> RequireStateAsync(CancellationToken cancellationToken)
        => await stateStore.LoadAsync(composeFile.ProjectName, cancellationToken)
            ?? throw new InvalidOperationException($"Project '{composeFile.ProjectName}' is not up. Run 'up' first.");

    private static string RequireContainerId(ProjectState state, string serviceName)
        => state.ContainerIdsByService.TryGetValue(serviceName, out var containerId)
            ? containerId
            : throw new InvalidOperationException($"Service '{serviceName}' is not part of project '{state.ProjectName}'.");
}
