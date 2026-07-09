using System.Text.Json;

namespace WslContainerCompose.Core.State;

/// <summary>
/// Reads/writes the local per-project state file, e.g. `.wsl-compose/&lt;project&gt;.json`.
/// </summary>
public sealed class ProjectStateStore(string stateDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string GetStatePath(string projectName) => Path.Combine(stateDirectory, $"{projectName}.json");

    public async Task SaveAsync(ProjectState state, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(stateDirectory);
        await using var stream = File.Create(GetStatePath(state.ProjectName));
        await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
    }

    public async Task<ProjectState?> LoadAsync(string projectName, CancellationToken cancellationToken = default)
    {
        var path = GetStatePath(projectName);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ProjectState>(stream, JsonOptions, cancellationToken);
    }

    public void Delete(string projectName)
    {
        var path = GetStatePath(projectName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
