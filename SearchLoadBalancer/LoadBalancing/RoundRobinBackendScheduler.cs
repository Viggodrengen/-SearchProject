using System.Threading;

namespace SearchLoadBalancer.LoadBalancing;

public class RoundRobinBackendScheduler : IBackendScheduler
{
    private int _counter = -1;

    public string Name => "round-robin";

    public int GetStartIndex(int backendCount)
    {
        if (backendCount <= 0)
        {
            return 0;
        }

        var value = Interlocked.Increment(ref _counter);
        return Math.Abs(value % backendCount);
    }
}
