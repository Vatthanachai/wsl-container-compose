using WslContainerCompose.Core.Compose;

namespace WslContainerCompose.Core.Orchestration;

/// <summary>
/// Resolves which services can reach each other by name. Network membership is a logical
/// grouping only - it is not enforced isolation, since the WSL container API has no concept
/// of multiple isolated networks (only a single None/Bridged mode). See Plan.md
/// "Networks (provisional)".
/// </summary>
public static class NetworkTopology
{
    private const string DefaultNetwork = "default";

    /// <summary>
    /// A service with no declared `networks:` joins an implicit default network shared with
    /// every other network-less service. A service with explicit `networks:` joins only those.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlySet<string>> ResolveMembership(
        IReadOnlyDictionary<string, ServiceDefinition> services)
    {
        var membership = new Dictionary<string, IReadOnlySet<string>>();
        foreach (var (name, service) in services)
        {
            membership[name] = service.Networks.Count == 0
                ? new HashSet<string> { DefaultNetwork }
                : new HashSet<string>(service.Networks);
        }

        return membership;
    }

    /// <summary>
    /// For each service, the set of other services that share at least one network with it.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlySet<string>> ResolvePeers(
        IReadOnlyDictionary<string, IReadOnlySet<string>> membership)
    {
        var peers = new Dictionary<string, IReadOnlySet<string>>();
        foreach (var (name, networks) in membership)
        {
            var peerNames = new HashSet<string>();
            foreach (var (otherName, otherNetworks) in membership)
            {
                if (otherName != name && networks.Overlaps(otherNetworks))
                {
                    peerNames.Add(otherName);
                }
            }

            peers[name] = peerNames;
        }

        return peers;
    }
}
