using System.CommandLine;
using WslContainerCompose.Core.Compose;
using WslContainerCompose.Core.Orchestration;
using WslContainerCompose.Core.Runtime;
using WslContainerCompose.Core.State;

var fileOption = new Option<FileInfo>("--file", "-f")
{
    Description = "Path to the compose file.",
    DefaultValueFactory = _ => new FileInfo("docker-compose.yml"),
};

var projectNameOption = new Option<string?>("--project-name", "-p")
{
    Description = "Project name. Defaults to the compose file's directory name.",
};

var rootCommand = new RootCommand("wsl-compose - a docker-compose-like tool for WSL containers")
{
    fileOption,
    projectNameOption,
};

var serviceArgument = new Argument<string>("service")
{
    Description = "Service name, as declared in the compose file.",
};

var upCommand = new Command("up", "Create and start all services.");
var downCommand = new Command("down", "Stop and remove all services.");
var psCommand = new Command("ps", "List this project's containers.");
var logsCommand = new Command("logs", "Show logs for a service.") { serviceArgument };
var stopCommand = new Command("stop", "Stop all services without removing them.");
var startCommand = new Command("start", "Start previously-stopped services.");
var restartCommand = new Command("restart", "Stop then start all services.");

rootCommand.Subcommands.Add(upCommand);
rootCommand.Subcommands.Add(downCommand);
rootCommand.Subcommands.Add(psCommand);
rootCommand.Subcommands.Add(logsCommand);
rootCommand.Subcommands.Add(stopCommand);
rootCommand.Subcommands.Add(startCommand);
rootCommand.Subcommands.Add(restartCommand);

upCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var project = LoadProject(parseResult);
    var result = await project.UpAsync(cancellationToken);

    foreach (var (service, containerId) in result.ContainerIdsByService)
    {
        Console.WriteLine($"{service}: {containerId}");
    }

    foreach (var (service, error) in result.Failures)
    {
        Console.Error.WriteLine($"{service}: failed - {error.Message}");
    }

    return result.Failures.Count == 0 ? 0 : 1;
});

downCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await LoadProject(parseResult).DownAsync(cancellationToken);
    return 0;
});

psCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var statuses = await LoadProject(parseResult).PsAsync(cancellationToken);
    foreach (var status in statuses)
    {
        Console.WriteLine($"{status.Name}\t{status.ContainerId}\t{(status.IsRunning ? "running" : "stopped")}");
    }

    return 0;
});

logsCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var service = parseResult.GetValue(serviceArgument)!;
    var lines = await LoadProject(parseResult).LogsAsync(service, cancellationToken);
    foreach (var line in lines)
    {
        Console.WriteLine(line);
    }

    return 0;
});

stopCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await LoadProject(parseResult).StopAsync(cancellationToken);
    return 0;
});

startCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await LoadProject(parseResult).StartAsync(cancellationToken);
    return 0;
});

restartCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await LoadProject(parseResult).RestartAsync(cancellationToken);
    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();

ComposeProject LoadProject(ParseResult parseResult)
{
    var file = parseResult.GetValue(fileOption)!;
    var projectName = parseResult.GetValue(projectNameOption)
        ?? file.Directory?.Name
        ?? "wsl-compose";

    var yaml = File.ReadAllText(file.FullName);
    var envFilePath = Path.Combine(file.DirectoryName ?? ".", ".env");
    var environment = EnvInterpolation.LoadEnvironment(envFilePath);
    var composeFile = ComposeParser.Parse(yaml, projectName, environment);

    var stateDirectory = Path.Combine(file.DirectoryName ?? ".", ".wsl-compose");
    var stateStore = new ProjectStateStore(stateDirectory);

    // TODO: swap in the real Microsoft.WSL.Containers adapter once it's written.
    IContainerRuntime runtime = new NotImplementedContainerRuntime();

    return new ComposeProject(composeFile, runtime, stateStore);
}
