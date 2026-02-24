namespace SearchLoadBalancer.LoadBalancing;

public class BackendSchedulerFactory
{
    private readonly IEnumerable<IBackendScheduler> _schedulers;

    public BackendSchedulerFactory(IEnumerable<IBackendScheduler> schedulers)
    {
        _schedulers = schedulers;
    }

    public IBackendScheduler Create(string? strategy)
    {
        var desired = strategy?.Trim();
        if (string.IsNullOrWhiteSpace(desired))
        {
            desired = "round-robin";
        }

        var scheduler = _schedulers.FirstOrDefault(x =>
            string.Equals(x.Name, desired, StringComparison.OrdinalIgnoreCase));

        if (scheduler is not null)
        {
            return scheduler;
        }

        return _schedulers.First(x =>
            string.Equals(x.Name, "round-robin", StringComparison.OrdinalIgnoreCase));
    }
}
