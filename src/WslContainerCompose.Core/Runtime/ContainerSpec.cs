using WslContainerCompose.Core.Compose;

namespace WslContainerCompose.Core.Runtime;

public sealed record ContainerSpec(
    string Name,
    string Image,
    IReadOnlyList<string> Command,
    IReadOnlyDictionary<string, string> Environment,
    IReadOnlyList<PortMapping> Ports,
    IReadOnlyList<BindMount> Volumes);

public sealed record ContainerStatus(string ContainerId, string Name, bool IsRunning);
