namespace WslContainerCompose.Core.Compose;

/// <summary>
/// Thrown for both malformed compose files and compose directives that v1 deliberately
/// doesn't support yet (see obsidian/wsl-container-compose/Plan.md).
/// </summary>
public sealed class ComposeParseException(string message) : Exception(message);
