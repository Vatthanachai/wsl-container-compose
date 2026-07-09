namespace WslContainerCompose.Core.Compose;

public sealed record PortMapping(int HostPort, int ContainerPort);

public sealed record BindMount(string HostPath, string ContainerPath);

public sealed class ServiceDefinition
{
    public required string Name { get; init; }
    public required string Image { get; init; }
    public IReadOnlyList<string> Command { get; init; } = [];
    public IReadOnlyList<PortMapping> Ports { get; init; } = [];
    public IReadOnlyList<BindMount> Volumes { get; init; } = [];
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> DependsOn { get; init; } = [];
}
