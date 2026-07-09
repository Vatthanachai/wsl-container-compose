using WslContainerCompose.Core.Compose;

namespace WslContainerCompose.Core.Orchestration;

/// <summary>
/// `depends_on` in v1 determines start/stop order only - no health-check waiting
/// (see Plan.md "Startup ordering").
/// </summary>
public static class DependencyOrder
{
    public static IReadOnlyList<string> ResolveStartOrder(IReadOnlyDictionary<string, ServiceDefinition> services)
    {
        var order = new List<string>();
        var visiting = new HashSet<string>();
        var visited = new HashSet<string>();

        foreach (var name in services.Keys)
        {
            Visit(name);
        }

        return order;

        void Visit(string name)
        {
            if (visited.Contains(name))
            {
                return;
            }

            if (!visiting.Add(name))
            {
                throw new InvalidOperationException($"Circular 'depends_on' involving service '{name}'.");
            }

            if (services.TryGetValue(name, out var service))
            {
                foreach (var dependency in service.DependsOn)
                {
                    if (!services.ContainsKey(dependency))
                    {
                        throw new InvalidOperationException($"Service '{name}' depends_on unknown service '{dependency}'.");
                    }

                    Visit(dependency);
                }
            }

            visiting.Remove(name);
            visited.Add(name);
            order.Add(name);
        }
    }

    public static IReadOnlyList<string> ResolveStopOrder(IReadOnlyDictionary<string, ServiceDefinition> services)
        => [.. ResolveStartOrder(services).Reverse()];
}
