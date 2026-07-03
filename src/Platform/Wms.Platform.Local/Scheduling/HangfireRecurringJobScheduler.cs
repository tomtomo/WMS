using Hangfire;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Scheduling;

// Cron durable via Hangfire + Postgres (cloud: Functions timer / Cloud Scheduler).
public sealed class HangfireRecurringJobScheduler(IRecurringJobManager recurringJobManager)
    : IRecurringJobScheduler
{
    public Task ScheduleRecurringAsync<TJob>(string jobId, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : IRecurringJob
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

        recurringJobManager.AddOrUpdate<TJob>(jobId, job => job.ExecuteAsync(CancellationToken.None), cronExpression);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string jobId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        recurringJobManager.RemoveIfExists(jobId);
        return Task.CompletedTask;
    }
}
