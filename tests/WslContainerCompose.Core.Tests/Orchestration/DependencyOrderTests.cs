using WslContainerCompose.Core.Compose;
using WslContainerCompose.Core.Orchestration;

namespace WslContainerCompose.Core.Tests.Orchestration;

public class DependencyOrderTests
{
    [Fact]
    public void Start_order_puts_dependencies_before_dependents()
    {
        var services = new Dictionary<string, ServiceDefinition>
        {
            ["web"] = Service("web", dependsOn: ["api"]),
            ["api"] = Service("api", dependsOn: ["db"]),
            ["db"] = Service("db"),
        };

        var order = DependencyOrder.ResolveStartOrder(services).ToList();

        Assert.True(order.IndexOf("db") < order.IndexOf("api"));
        Assert.True(order.IndexOf("api") < order.IndexOf("web"));
    }

    [Fact]
    public void Stop_order_is_the_reverse_of_start_order()
    {
        var services = new Dictionary<string, ServiceDefinition>
        {
            ["web"] = Service("web", dependsOn: ["api"]),
            ["api"] = Service("api"),
        };

        var startOrder = DependencyOrder.ResolveStartOrder(services);
        var stopOrder = DependencyOrder.ResolveStopOrder(services);

        Assert.Equal(startOrder.Reverse(), stopOrder);
    }

    [Fact]
    public void Throws_on_circular_dependency()
    {
        var services = new Dictionary<string, ServiceDefinition>
        {
            ["a"] = Service("a", dependsOn: ["b"]),
            ["b"] = Service("b", dependsOn: ["a"]),
        };

        Assert.Throws<InvalidOperationException>(() => DependencyOrder.ResolveStartOrder(services));
    }

    [Fact]
    public void Throws_on_unknown_dependency()
    {
        var services = new Dictionary<string, ServiceDefinition>
        {
            ["a"] = Service("a", dependsOn: ["missing"]),
        };

        Assert.Throws<InvalidOperationException>(() => DependencyOrder.ResolveStartOrder(services));
    }

    private static ServiceDefinition Service(string name, IReadOnlyList<string>? dependsOn = null) => new()
    {
        Name = name,
        Image = "irrelevant",
        DependsOn = dependsOn ?? [],
    };
}
