using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.Scheduling;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Kontrak scheduling Azure offline
public sealed class SchedulingParityTests
{
    private static readonly DateTimeOffset _dueAt = new(2026, 7, 11, 8, 0, 0, TimeSpan.Zero);

    private readonly ServiceBusSender _sender = Substitute.For<ServiceBusSender>();
    private readonly ServiceBusScheduledDelayedTaskQueue _queue;

    public SchedulingParityTests()
    {
        var client = Substitute.For<ServiceBusClient>();
        client.CreateSender("wms-delayed-tasks").Returns(_sender);
        _queue = new ServiceBusScheduledDelayedTaskQueue(client, Options.Create(new AzureMessagingOptions()));
    }

    [Fact]
    public async Task Schedule_returns_the_broker_sequence_number_as_task_id()
    {
        ServiceBusMessage? scheduled = null;
        _sender.ScheduleMessageAsync(Arg.Do<ServiceBusMessage>(message => scheduled = message), _dueAt, Arg.Any<CancellationToken>())
            .Returns(42L);

        var taskId = await _queue.ScheduleAsync(new EscalateGrPayload(Guid.Parse("55555555-5555-5555-5555-555555555555")), _dueAt);

        taskId.Should().Be("42");
        scheduled.Should().NotBeNull();
        scheduled!.Subject.Should().Be(nameof(EscalateGrPayload));
        scheduled.ContentType.Should().Be("application/json");
        scheduled.ApplicationProperties["payloadType"].Should().Be(typeof(EscalateGrPayload).FullName);
        scheduled.Body.ToString().Should().Contain("55555555-5555-5555-5555-555555555555");
    }

    [Fact]
    public async Task Cancel_translates_the_task_id_back_to_the_scheduled_sequence_number()
    {
        await _queue.CancelAsync("42");

        await _sender.Received(1).CancelScheduledMessageAsync(42L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_after_the_task_already_fired_is_idempotent()
    {
        _sender.CancelScheduledMessageAsync(42L, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ServiceBusException("gone", ServiceBusFailureReason.MessageNotFound)));

        var cancel = async () => await _queue.CancelAsync("42");

        await cancel.Should().NotThrowAsync("parity Hangfire Delete: cancel task yang sudah fire bukan error");
    }

    [Theory]
    [InlineData("0 0 2 * * *")]
    [InlineData("0 2 * * *")]
    public async Task Valid_ncrontab_registers_the_job(string cron)
    {
        var scheduler = new FunctionsTimerRecurringJobScheduler();

        await scheduler.ScheduleRecurringAsync<FakeRecurringJob>("expiry-scan", cron);

        scheduler.Jobs.Should().ContainSingle(job =>
            job.JobId == "expiry-scan" && job.JobType == typeof(FakeRecurringJob) && job.CronExpression == cron);
    }

    [Fact]
    public async Task Invalid_cron_expression_is_rejected()
    {
        var scheduler = new FunctionsTimerRecurringJobScheduler();

        var schedule = async () => await scheduler.ScheduleRecurringAsync<FakeRecurringJob>("expiry-scan", "tiap subuh");

        await schedule.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Re_registering_the_same_job_id_upserts_the_schedule()
    {
        var scheduler = new FunctionsTimerRecurringJobScheduler();

        await scheduler.ScheduleRecurringAsync<FakeRecurringJob>("expiry-scan", "0 2 * * *");
        await scheduler.ScheduleRecurringAsync<FakeRecurringJob>("expiry-scan", "0 3 * * *");

        scheduler.Jobs.Should().ContainSingle(job => job.CronExpression == "0 3 * * *");
    }

    [Fact]
    public async Task Remove_is_idempotent()
    {
        var scheduler = new FunctionsTimerRecurringJobScheduler();
        await scheduler.ScheduleRecurringAsync<FakeRecurringJob>("expiry-scan", "0 2 * * *");

        await scheduler.RemoveAsync("expiry-scan");
        await scheduler.RemoveAsync("expiry-scan");

        scheduler.Jobs.Should().BeEmpty();
    }

    private sealed record EscalateGrPayload(Guid GrId);

    private sealed class FakeRecurringJob : IRecurringJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
