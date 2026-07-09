using System.Text.RegularExpressions;

namespace WslContainerCompose.Core.Compose;

/// <summary>
/// Supports the `${VAR}` / `${VAR:-default}` forms only (see Plan.md) - not bare `$VAR`.
/// </summary>
public static partial class EnvInterpolation
{
    [GeneratedRegex(@"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)(?<hasDefault>:-(?<default>[^}]*))?\}")]
    private static partial Regex VariablePattern();

    public static string Interpolate(string source, IReadOnlyDictionary<string, string> environment)
    {
        return VariablePattern().Replace(source, match =>
        {
            var name = match.Groups["name"].Value;
            if (environment.TryGetValue(name, out var value))
            {
                return value;
            }

            return match.Groups["hasDefault"].Success ? match.Groups["default"].Value : string.Empty;
        });
    }

    /// <summary>
    /// Loads `.env` (if present) then overlays OS environment variables, matching docker compose's
    /// precedence: shell environment wins over the `.env` file.
    /// </summary>
    public static IReadOnlyDictionary<string, string> LoadEnvironment(string? envFilePath)
    {
        var result = new Dictionary<string, string>();

        if (envFilePath is not null && File.Exists(envFilePath))
        {
            foreach (var rawLine in File.ReadLines(envFilePath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim().Trim('"');
                result[key] = value;
            }
        }

        foreach (System.Collections.DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
        {
            result[(string)entry.Key] = (string?)entry.Value ?? string.Empty;
        }

        return result;
    }
}
