using AwesomeAssertions;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

// Durable dibuktikan dari provider kedua di atas DB yang sama. Tidak ada Hangfire server yang jalan, jadi tak ada eksekusi, murni registrasi dan persistence.
[Collection(PostgresCollection.Name)]
public sealed class HangfireSchedulingTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Recurring_job_registration_is_durable_across_providers()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync();
        using (var provider = BuildLocalPlatformProvider(connectionString))
        {
            var scheduler = provider.GetRequiredService<IRecurringJobScheduler>();
            await scheduler.ScheduleRecurringAsync<FakeRecurringJob>("expiry-scan", "*/5 * * * *");
        }

        using var secondProvider = BuildLocalPlatformProvider(connectionString);
        using var connection = secondProvider.GetRequiredService<JobStorage>().GetConnection();

        connection.GetRecurringJobs().Should().Contain(job => job.Id == "expiry-scan");
    }

    [Fact]
    public async Task Removed_recurring_job_disappears_from_storage()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync();
        using var provider = BuildLocalPlatformProvider(connectionString);
        var scheduler = provider.GetRequiredService<IRecurringJobScheduler>();
        await scheduler.ScheduleRecurringAsync<FakeRecurringJob>("expiry-scan", "*/5 * * * *");

        await scheduler.RemoveAsync("expiry-scan");

        using var connection = provider.GetRequiredService<JobStorage>().GetConnection();
        connection.GetRecurringJobs().Should().NotContain(job => job.Id == "expiry-scan");
    }

    [Fact]
    public async Task Delayed_task_enqueue_is_durable_across_providers()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync();
        string taskId;
        using (var provider = BuildLocalPlatformProvider(connectionString))
        {
            var queue = provider.GetRequiredService<IDelayedTaskQueue>();
            taskId = await queue.ScheduleAsync(new PingPayload("sla-escalation"), DateTimeOffset.UtcNow.AddHours(1));
        }

        taskId.Should().NotBeNullOrWhiteSpace();
        using var secondProvider = BuildLocalPlatformProvider(connectionString);
        var monitoring = secondProvider.GetRequiredService<JobStorage>().GetMonitoringApi();

        monitoring.ScheduledJobs(0, 50).Should().Contain(job => job.Key == taskId);
    }

    [Fact]
    public async Task Cancelled_delayed_task_leaves_scheduled_set()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync();
        using var provider = BuildLocalPlatformProvider(connectionString);
        var queue = provider.GetRequiredService<IDelayedTaskQueue>();
        var taskId = await queue.ScheduleAsync(new PingPayload("sla-escalation"), DateTimeOffset.UtcNow.AddHours(1));

        await queue.CancelAsync(taskId);

        var monitoring = provider.GetRequiredService<JobStorage>().GetMonitoringApi();
        monitoring.ScheduledJobs(0, 50).Should().NotContain(job => job.Key == taskId);
    }

    private static ServiceProvider BuildLocalPlatformProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("ConnectionStrings:wms", connectionString),
                new("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672"),
                new("LocalPlatform:ObjectStore:RootPath", Path.Combine(Path.GetTempPath(), "wms-hangfire-objstore")),
                new("LocalPlatform:ObjectStore:BaseUrl", "http://localhost:5099/objects"),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLocalPlatform(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
