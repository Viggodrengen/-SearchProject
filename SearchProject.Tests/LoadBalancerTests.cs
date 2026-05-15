using SearchLoadBalancer.LoadBalancing;
using Xunit;

namespace SearchProject.Tests;

public class LoadBalancerTests
{
    [Fact]
    public void RoundRobinScheduler_CyclesThroughBackends()
    {
        var scheduler = new RoundRobinBackendScheduler();

        var sequence = Enumerable.Range(0, 5)
            .Select(_ => scheduler.GetStartIndex(3))
            .ToArray();

        Assert.Equal([0, 1, 2, 0, 1], sequence);
    }

    [Fact]
    public void RoundRobinScheduler_ReturnsZeroWhenNoBackendsExist()
    {
        var scheduler = new RoundRobinBackendScheduler();

        Assert.Equal(0, scheduler.GetStartIndex(0));
        Assert.Equal(0, scheduler.GetStartIndex(-1));
    }

    [Fact]
    public void BackendSchedulerFactory_DefaultsToRoundRobinForMissingOrUnknownStrategy()
    {
        var roundRobin = new RoundRobinBackendScheduler();
        var random = new RandomBackendScheduler();
        var factory = new BackendSchedulerFactory([roundRobin, random]);

        Assert.Same(roundRobin, factory.Create(null));
        Assert.Same(roundRobin, factory.Create(""));
        Assert.Same(roundRobin, factory.Create("does-not-exist"));
    }

    [Fact]
    public void BackendSchedulerFactory_SelectsStrategyCaseInsensitively()
    {
        var roundRobin = new RoundRobinBackendScheduler();
        var random = new RandomBackendScheduler();
        var factory = new BackendSchedulerFactory([roundRobin, random]);

        Assert.Same(random, factory.Create("RANDOM"));
        Assert.Same(roundRobin, factory.Create("ROUND-ROBIN"));
    }
}
