using YamlDotNet.Serialization;

namespace WslContainerCompose.Core.Compose;

/// <summary>
/// Parses the v1-supported subset of the Compose Specification (see Plan.md for exactly
/// what's in and out of scope: no `build:`, no named volumes, no `networks:` topology).
/// </summary>
public static class ComposeParser
{
    public static ComposeFile Parse(string yaml, string projectName, IReadOnlyDictionary<string, string>? environment = null)
    {
        var interpolated = EnvInterpolation.Interpolate(yaml, environment ?? new Dictionary<string, string>());

        var deserializer = new DeserializerBuilder().Build();
        var root = deserializer.Deserialize<Dictionary<object, object?>>(interpolated)
            ?? throw new ComposeParseException("Compose file is empty.");

        if (GetValue(root, "volumes") is Dictionary<object, object?> { Count: > 0 })
        {
            throw new ComposeParseException(
                "Top-level 'volumes:' (named volumes) is not supported yet - use bind mounts instead.");
        }

        if (GetValue(root, "services") is not Dictionary<object, object?> rawServices)
        {
            throw new ComposeParseException("Compose file has no 'services:' section.");
        }

        var declaredNetworks = ParseTopLevelNetworks(GetValue(root, "networks"));

        var services = new Dictionary<string, ServiceDefinition>();
        foreach (var (key, value) in rawServices)
        {
            var name = key.ToString()!;
            services[name] = ParseService(name, value as Dictionary<object, object?> ?? [], declaredNetworks);
        }

        return new ComposeFile { ProjectName = projectName, Services = services, Networks = declaredNetworks };
    }

    private static IReadOnlySet<string> ParseTopLevelNetworks(object? raw)
    {
        if (raw is not Dictionary<object, object?> map)
        {
            return new HashSet<string>();
        }

        var names = new HashSet<string>();
        foreach (var (key, value) in map)
        {
            var name = key.ToString()!;
            if (value is Dictionary<object, object?> { Count: > 0 } options)
            {
                throw new ComposeParseException(
                    $"Network '{name}' specifies options ({string.Join(", ", options.Keys)}), which are not supported yet - only bare network names are supported.");
            }

            names.Add(name);
        }

        return names;
    }

    private static ServiceDefinition ParseService(string name, Dictionary<object, object?> raw, IReadOnlySet<string> declaredNetworks)
    {
        if (raw.ContainsKey("build"))
        {
            throw new ComposeParseException(
                $"Service '{name}' uses 'build:', which is not supported yet - v1 requires 'image:'.");
        }

        if (GetValue(raw, "image") is not string image)
        {
            throw new ComposeParseException($"Service '{name}' has no 'image:'.");
        }

        return new ServiceDefinition
        {
            Name = name,
            Image = image,
            Command = ParseCommand(GetValue(raw, "command")),
            Ports = ParsePorts(name, GetValue(raw, "ports")),
            Volumes = ParseVolumes(name, GetValue(raw, "volumes")),
            Environment = ParseEnvironment(GetValue(raw, "environment")),
            DependsOn = ParseDependsOn(GetValue(raw, "depends_on")),
            Networks = ParseServiceNetworks(name, GetValue(raw, "networks"), declaredNetworks),
        };
    }

    private static object? GetValue(Dictionary<object, object?> map, string key)
        => map.TryGetValue(key, out var value) ? value : null;

    private static IReadOnlyList<string> ParseCommand(object? raw) => raw switch
    {
        null => [],
        string single => [single],
        List<object?> list => [.. list.Select(item => item?.ToString() ?? string.Empty)],
        _ => throw new ComposeParseException("'command:' must be a string or a list of strings."),
    };

    private static IReadOnlyList<PortMapping> ParsePorts(string serviceName, object? raw)
    {
        if (raw is not List<object?> list)
        {
            return [];
        }

        var ports = new List<PortMapping>();
        foreach (var entry in list)
        {
            var text = entry?.ToString() ?? throw new ComposeParseException($"Service '{serviceName}' has an empty port mapping.");
            // Strip an optional "/tcp" or "/udp" suffix - v1 doesn't distinguish protocols.
            var slashIndex = text.IndexOf('/');
            if (slashIndex >= 0)
            {
                text = text[..slashIndex];
            }

            var parts = text.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var hostPort) || !int.TryParse(parts[1], out var containerPort))
            {
                throw new ComposeParseException(
                    $"Service '{serviceName}' has an unsupported port mapping '{text}' - expected 'host:container'.");
            }

            ports.Add(new PortMapping(hostPort, containerPort));
        }

        return ports;
    }

    private static IReadOnlyList<BindMount> ParseVolumes(string serviceName, object? raw)
    {
        if (raw is not List<object?> list)
        {
            return [];
        }

        var volumes = new List<BindMount>();
        foreach (var entry in list)
        {
            var text = entry?.ToString() ?? throw new ComposeParseException($"Service '{serviceName}' has an empty volume entry.");
            var parts = text.Split(':');
            if (parts.Length < 2)
            {
                throw new ComposeParseException(
                    $"Service '{serviceName}' has an unsupported volume entry '{text}' - expected 'host/path:/container/path'.");
            }

            var hostPath = parts[0];
            var isBindMount = hostPath.StartsWith('.') || hostPath.StartsWith('/') || hostPath.Contains('\\')
                || (hostPath.Length > 1 && hostPath[1] == ':'); // e.g. C:\...

            if (!isBindMount)
            {
                throw new ComposeParseException(
                    $"Service '{serviceName}' references named volume '{hostPath}', which is not supported yet - use a bind mount instead.");
            }

            volumes.Add(new BindMount(hostPath, parts[1]));
        }

        return volumes;
    }

    private static IReadOnlyDictionary<string, string> ParseEnvironment(object? raw) => raw switch
    {
        null => new Dictionary<string, string>(),
        List<object?> list => list
            .Select(item => item?.ToString() ?? string.Empty)
            .Select(entry => entry.Split('=', 2))
            .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : string.Empty),
        Dictionary<object, object?> map => map.ToDictionary(
            kvp => kvp.Key.ToString()!,
            kvp => kvp.Value?.ToString() ?? string.Empty),
        _ => throw new ComposeParseException("'environment:' must be a list or a map."),
    };

    private static IReadOnlyList<string> ParseDependsOn(object? raw) => raw switch
    {
        null => [],
        List<object?> list => [.. list.Select(item => item?.ToString() ?? string.Empty)],
        // Long form: `depends_on: { api: { condition: service_healthy } }`.
        // v1 only honors ordering (see Plan.md), so the condition value itself is discarded.
        Dictionary<object, object?> map => [.. map.Keys.Select(key => key.ToString()!)],
        _ => throw new ComposeParseException("'depends_on:' must be a list or a map."),
    };

    private static IReadOnlyList<string> ParseServiceNetworks(string serviceName, object? raw, IReadOnlySet<string> declaredNetworks)
    {
        IReadOnlyList<string> names = raw switch
        {
            null => [],
            List<object?> list => [.. list.Select(item => item?.ToString() ?? throw new ComposeParseException($"Service '{serviceName}' has an empty network entry."))],
            Dictionary<object, object?> map => ParseServiceNetworksMap(serviceName, map),
            _ => throw new ComposeParseException("'networks:' must be a list or a map."),
        };

        foreach (var networkName in names)
        {
            if (!declaredNetworks.Contains(networkName))
            {
                throw new ComposeParseException(
                    $"Service '{serviceName}' references network '{networkName}', which is not declared in the top-level 'networks:' block.");
            }
        }

        return names;
    }

    private static IReadOnlyList<string> ParseServiceNetworksMap(string serviceName, Dictionary<object, object?> map)
    {
        var names = new List<string>();
        foreach (var (key, value) in map)
        {
            var networkName = key.ToString()!;
            if (value is Dictionary<object, object?> { Count: > 0 } options)
            {
                throw new ComposeParseException(
                    $"Service '{serviceName}' network '{networkName}' specifies options ({string.Join(", ", options.Keys)}), which are not supported yet - only network membership is supported.");
            }

            names.Add(networkName);
        }

        return names;
    }
}
