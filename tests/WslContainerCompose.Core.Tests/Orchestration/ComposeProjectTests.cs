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
}
