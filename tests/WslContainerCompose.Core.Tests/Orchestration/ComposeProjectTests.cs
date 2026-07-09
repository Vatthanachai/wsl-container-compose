using WslContainerCompose.Core.Compose;
using WslContainerCompose.Core.Orchestration;
using WslContainerCompose.Core.State;

namespace WslContainerCompose.Core.Tests.Orchestration;

public class ComposeProjectTests : IDisposable
{
    private readonly string _stateDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_stateDirectory))
        {
            Directory.Delete(_stateDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Up_starts_every_service_and_persists_state()
    {
        var composeFile = new ComposeFile
        {
            ProjectName = "myproj",
            Services = new Dictionary<string, ServiceDefinition>
            {
                ["db"] = new() { Name = "db", Image = "postgres" },
                ["api"] = new() { Name = "api", Image = "my-api", DependsOn = ["db"] },
            },
        };

        var runtime = new FakeContainerRuntime();
        var project = new ComposeProject(composeFile, runtime, new ProjectStateStore(_stateDirectory));

        var result = await project.UpAsync();

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.ContainerIdsByService.Count);
        Assert.Equal(2, runtime.StartedContainerIds.Count);
    }

    [Fact]
    public async Task Up_leaves_already_started_containers_running_when_a_later_service_fails()
    {
        var composeFile = new ComposeFile
        {
            ProjectName = "myproj",
            Services = new Dictionary<string, ServiceDefinition>
            {
                ["db"] = new() { Name = "db", Image = "postgres" },
                ["api"] = new() { Name = "api", Image = "broken-image", DependsOn = ["db"] },
            },
        };

        var runtime = new FakeContainerRuntime { ImagesThatFailToPull = { "broken-image" } };
        var project = new ComposeProject(composeFile, runtime, new ProjectStateStore(_stateDirectory));

        var result = await project.UpAsync();

        var failure = Assert.Single(result.Failures);
        Assert.Equal("api", failure.Service);
        Assert.Single(result.ContainerIdsByService); // db still recorded as up
        Assert.Single(runtime.StartedContainerIds); // db's container was started
    }

    [Fact]
    public async Task Down_stops_and_deletes_containers_in_reverse_order_and_clears_state()
    {
        var composeFile = new ComposeFile
        {
            ProjectName = "myproj",
            Services = new Dictionary<string, ServiceDefinition>
            {
                ["db"] = new() { Name = "db", Image = "postgres" },
                ["api"] = new() { Name = "api", Image = "my-api", DependsOn = ["db"] },
            },
        };

        var runtime = new FakeContainerRuntime();
        var stateStore = new ProjectStateStore(_stateDirectory);
        var project = new ComposeProject(composeFile, runtime, stateStore);

        await project.UpAsync();
        await project.DownAsync();

        Assert.Equal(2, runtime.StoppedContainerIds.Count);
        Assert.Equal(2, runtime.DeletedContainerIds.Count);
        Assert.True(runtime.SessionTerminated);
        Assert.Null(await stateStore.LoadAsync("myproj"));
    }

    [Fact]
    public async Task Stop_before_up_throws()
    {
        var composeFile = new ComposeFile { ProjectName = "myproj", Services = new Dictionary<string, ServiceDefinition>() };
        var project = new ComposeProject(composeFile, new FakeContainerRuntime(), new ProjectStateStore(_stateDirectory));

        await Assert.ThrowsAsync<InvalidOperationException>(() => project.StopAsync());
    }

    [Fact]
    public async Task Up_wires_hosts_entries_for_services_on_the_implicit_default_network()
    {
        var composeFile = new ComposeFile
        {
            ProjectName = "myproj",
            Services = new Dictionary<string, ServiceDefinition>
            {
                ["web"] = new() { Name = "web", Image = "nginx" },
                ["api"] = new() { Name = "api", Image = "my-api" },
            },
        };

        var runtime = new FakeContainerRuntime();
        var project = new ComposeProject(composeFile, runtime, new ProjectStateStore(_stateDirectory));

        var result = await project.UpAsync();

        Assert.Empty(result.Failures);
        var webContainerId = result.ContainerIdsByService["web"];
        var apiContainerId = result.ContainerIdsByService["api"];
        var apiIp = await runtime.GetContainerIpAddressAsync("irrelevant", apiContainerId);
        var webIp = await runtime.GetContainerIpAddressAsync("irrelevant", webContainerId);

        Assert.Equal(apiIp, runtime.HostsEntriesByContainerId[webContainerId]["api"]);
        Assert.Equal(webIp, runtime.HostsEntriesByContainerId[apiContainerId]["web"]);
    }

    [Fact]
    public async Task Up_does_not_wire_hosts_entries_between_services_on_disjoint_networks()
    {
        var composeFile = new ComposeFile
        {
            ProjectName = "myproj",
            Networks = new HashSet<string> { "frontend", "backend" },
            Services = new Dictionary<string, ServiceDefinition>
            {
                ["web"] = new() { Name = "web", Image = "nginx", Networks = ["frontend"] },
                ["db"] = new() { Name = "db", Image = "postgres", Networks = ["backend"] },
            },
        };

        var runtime = new FakeContainerRuntime();
        var project = new ComposeProject(composeFile, runtime, new ProjectStateStore(_stateDirectory));

        var result = await project.UpAsync();

        Assert.Empty(result.Failures);
        Assert.DoesNotContain(result.ContainerIdsByService["web"], runtime.HostsEntriesByContainerId.Keys);
        Assert.DoesNotContain(result.ContainerIdsByService["db"], runtime.HostsEntriesByContainerId.Keys);
    }

    [Fact]
    public async Task Up_reports_a_soft_failure_when_ip_lookup_fails_but_leaves_the_container_running()
    {
        var composeFile = new ComposeFile
        {
            ProjectName = "myproj",
            Services = new Dictionary<string, ServiceDefinition>
            {
                ["db"] = new() { Name = "db", Image = "postgres" },
                ["api"] = new() { Name = "api", Image = "my-api", DependsOn = ["db"] },
            },
        };

        // "db" is created first (it has no dependencies), so it gets container-1.
        var runtime = new FakeContainerRuntime();
        runtime.ContainerIdsThatFailIpLookup.Add("container-1");
        var project = new ComposeProject(composeFile, runtime, new ProjectStateStore(_stateDirectory));

        var result = await project.UpAsync();

        var failure = Assert.Single(result.Failures);
        Assert.Equal("db", failure.Service);
        Assert.Equal(2, result.ContainerIdsByService.Count); // both containers still recorded as up
        Assert.Equal(2, runtime.StartedContainerIds.Count); // both containers were started
    }

    [Fact]
    public async Task Up_reports_a_soft_failure_when_writing_hosts_entries_fails()
    {
        var composeFile = new ComposeFile
        {
            ProjectName = "myproj",
            Services = new Dictionary<string, ServiceDefinition>
            {
                ["db"] = new() { Name = "db", Image = "postgres" },
                ["api"] = new() { Name = "api", Image = "my-api", DependsOn = ["db"] },
            },
        };

        var runtime = new FakeContainerRuntime();
        runtime.ContainerIdsThatFailHostsWrite.Add("container-1");
        var project = new ComposeProject(composeFile, runtime, new ProjectStateStore(_stateDirectory));

        var result = await project.UpAsync();

        var failure = Assert.Single(result.Failures);
        Assert.Equal("db", failure.Service);
        Assert.Equal(2, result.ContainerIdsByService.Count);
    }

    [Fact]
    public async Task Start_rewires_hosts_entries()
    {
        var composeFile = new ComposeFile
        {
            ProjectName = "myproj",
            Services = new Dictionary<string, ServiceDefinition>
            {
                ["web"] = new() { Name = "web", Image = "nginx" },
                ["api"] = new() { Name = "api", Image = "my-api" },
            },
        };

        var runtime = new FakeContainerRuntime();
        var project = new ComposeProject(composeFile, runtime, new ProjectStateStore(_stateDirectory));

        var result = await project.UpAsync();
        runtime.HostsEntriesByContainerId.Clear();

        await project.StartAsync();

        var webContainerId = result.ContainerIdsByService["web"];
        Assert.Contains("api", runtime.HostsEntriesByContainerId[webContainerId].Keys);
    }
}
