namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// scheduler cron : Hangfire recurring Local, Functions timer Azure, Cloud Scheduler GCP.
public interface IRecurringJobScheduler
{
    Task ScheduleRecurringAsync<TJob>(string jobId, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : IRecurringJob;

    Task RemoveAsync(string jobId, CancellationToken cancellationToken = default);
}

// Unit job yang dijalankan scheduler cron.
public interface IRecurringJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
