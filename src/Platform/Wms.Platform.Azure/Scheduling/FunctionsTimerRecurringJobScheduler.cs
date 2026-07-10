using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Scheduling;

// Registrasi cron sisi app: katalog job durable yang pemicunya timer trigger Functions
public sealed partial class FunctionsTimerRecurringJobScheduler : IRecurringJobScheduler
{
    private readonly ConcurrentDictionary<string, RecurringJobRegistration> _jobs = new(StringComparer.Ordinal);

    public IReadOnlyCollection<RecurringJobRegistration> Jobs => [.. _jobs.Values];

    public Task ScheduleRecurringAsync<TJob>(string jobId, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : IRecurringJob
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

        if (!IsValidNCrontab(cronExpression))
        {
            throw new ArgumentException(
                $"Ekspresi '{cronExpression}' bukan NCRONTAB valid",
                nameof(cronExpression));
        }

        _jobs[jobId] = new RecurringJobRegistration(jobId, typeof(TJob), cronExpression);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string jobId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        _jobs.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }

    private static bool IsValidNCrontab(string expression)
    {
        var fields = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return fields.Length is 5 or 6 && Array.TrueForAll(fields, field => CronFieldRegex().IsMatch(field));
    }

    [GeneratedRegex(@"^[0-9A-Za-z*,/\-?LW#]+$")]
    private static partial Regex CronFieldRegex();
}

// Satu entri katalog job recurring: identitas, tipe job, dan jadwalnya.
public sealed record RecurringJobRegistration(string JobId, Type JobType, string CronExpression);
