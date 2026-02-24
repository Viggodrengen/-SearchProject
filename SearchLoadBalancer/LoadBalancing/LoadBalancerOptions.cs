namespace SearchLoadBalancer.LoadBalancing;

public class LoadBalancerOptions
{
    public const string SectionName = "LoadBalancer";

    public string Strategy { get; set; } = "round-robin";

    public int BackendTimeoutSeconds { get; set; } = 10;

    public List<BackendEndpoint> Backends { get; set; } = new();
}

public class BackendEndpoint
{
    public string Name { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;
}
