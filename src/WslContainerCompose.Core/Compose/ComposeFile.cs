namespace WslContainerCompose.Core.Compose;

public sealed class ComposeFile
{
    public required string ProjectName { get; init; }
    public required IReadOnlyDictionary<string, ServiceDefinition> Services { get; init; }
}
