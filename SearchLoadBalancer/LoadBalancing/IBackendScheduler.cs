namespace SearchLoadBalancer.LoadBalancing;

public interface IBackendScheduler
{
    string Name { get; }

    int GetStartIndex(int backendCount);
}
