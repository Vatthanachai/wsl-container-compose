using WslContainerCompose.Core.Compose;
using WslContainerCompose.Core.Orchestration;

namespace WslContainerCompose.Core.Tests.Orchestration;

public class NetworkTopologyTests
{
    [Fact]
    public void Services_with_no_networks_share_the_implicit_default_network()
    {
        var services = new Dictionary<string, ServiceDefinition>
        {
            ["web"] = Service("web"),
            ["api"] = Service("api"),
        };

        var peers = NetworkTopology.ResolvePeers(NetworkTopology.ResolveMembership(services));

        Assert.Equal(new HashSet<string> { "api" }, peers["web"]);
        Assert.Equal(new HashSet<string> { "web" }, peers["api"]);
    }

    [Fact]
    public void Services_on_disjoint_explicit_networks_are_not_peers()
    {
        var services = new Dictionary<string, ServiceDefinition>
        {
            ["web"] = Service("web", networks: ["frontend"]),
            ["db"] = Service("db", networks: ["backend"]),
        };

        var peers = NetworkTopology.ResolvePeers(NetworkTopology.ResolveMembership(services));

        Assert.Empty(peers["web"]);
        Assert.Empty(peers["db"]);
    }

    [Fact]
    public void Services_sharing_an_explicit_network_are_peers()
    {
        var services = new Dictionary<string, ServiceDefinition>
        {
            ["web"] = Service("web", networks: ["frontend"]),
            ["api"] = Service("api", networks: ["frontend", "backend"]),
            ["db"] = Service("db", networks: ["backend"]),
        };

        var peers = NetworkTopology.ResolvePeers(NetworkTopology.ResolveMembership(services));

        Assert.Equal(new HashSet<string> { "api" }, peers["web"]);
        Assert.Equal(new HashSet<string> { "web", "db" }, peers["api"]);
        Assert.Equal(new HashSet<string> { "api" }, peers["db"]);
    }

    [Fact]
    public void A_service_with_explicit_networks_does_not_get_the_implicit_default()
    {
        var services = new Dictionary<string, ServiceDefinition>
        {
            ["web"] = Service("web"),
            ["isolated"] = Service("isolated", networks: ["frontend"]),
        };

        var peers = NetworkTopology.ResolvePeers(NetworkTopology.ResolveMembership(services));

        Assert.Empty(peers["isolated"]);
        Assert.Empty(peers["web"]);
    }

    private static ServiceDefinition Service(string name, IReadOnlyList<string>? networks = null) => new()
    {
        Name = name,
        Image = "irrelevant",
        Networks = networks ?? [],
    };
}
