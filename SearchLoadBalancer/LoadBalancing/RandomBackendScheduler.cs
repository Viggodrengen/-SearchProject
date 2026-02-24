namespace SearchLoadBalancer.LoadBalancing;

public class RandomBackendScheduler : IBackendScheduler
{
    public string Name => "random";

    public int GetStartIndex(int backendCount)
    {
        if (backendCount <= 0)
        {
            return 0;
        }

        return Random.Shared.Next(backendCount);
    }
}
