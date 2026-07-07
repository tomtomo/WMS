using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.Choreography.IntegrationTests.TestSupport;
using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;
using Xunit;

namespace Wms.Choreography.IntegrationTests;

// Test dead letter pada alur eventing.
[Collection(ChoreographyCollection.Name)]
public sealed class DeadLetterTests(ChoreographyFixture fixture)
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task Transient_conflict_is_retried_and_recovers_without_being_dropped()
    {
        // Simulasikan konflik pada percobaan pertama, lalu berhasil pada percobaan berikutnya.
        var fault = new FaultInjector
        {
            Behavior = (_, attempt) => attempt < 2
                ? Result.Conflict(new Error("concurrency.conflict", "xmin transient"))
                : Result.Success(),
        };
        await using var consumer = await StartConsumerAsync(fault);

        await DeadLetterHost.PublishAsync(consumer, GRConfirmed.LogicalName, DeliveryClass.CoreFlow);

        await ChoreographyWorld.WaitUntilAsync(() => Task.FromResult(fault.Successes >= 1), _timeout);
        fault.Successes.Should().Be(1, "conflict transient diretry lalu sukses — event tak hilang");
        (await DeadLetterHost.DeadLetterCountAsync(consumer)).Should().Be(0, "sembuh via retry, bukan DLQ");
    }

    [Fact]
    public async Task Poison_consumer_is_dead_lettered_after_three_retries()
    {
        var fault = new FaultInjector { Behavior = (_, _) => throw new InvalidOperationException("poison boom") };
        await using var consumer = await StartConsumerAsync(fault);

        await DeadLetterHost.PublishAsync(consumer, GRConfirmed.LogicalName, DeliveryClass.CoreFlow);

        await ChoreographyWorld.WaitUntilAsync(async () => await DeadLetterHost.DeadLetterCountAsync(consumer) >= 1, _timeout);

        fault.Attempts.Should().Be(ConsumerDeadLetterPipeline.MaxAttempts, "inline retry ×3 sebelum DLQ");
        var deadLetters = await DeadLetterHost.DeadLettersAsync(consumer);
        deadLetters.Should().ContainSingle();
        deadLetters[0].Source.Should().Be(GRConfirmed.LogicalName);
        deadLetters[0].Error.Should().Contain("poison boom");
    }

    [Fact]
    public async Task Poison_does_not_block_queue_and_healthy_messages_are_processed()
    {
        const int Healthy = 3;
        var fault = new FaultInjector
        {
            Behavior = (envelope, _) => envelope.Payload.Contains("POISON", StringComparison.Ordinal)
                ? throw new InvalidOperationException("poison")
                : Result.Success(),
        };
        await using var consumer = await StartConsumerAsync(fault);

        await DeadLetterHost.PublishAsync(consumer, GRConfirmed.LogicalName, DeliveryClass.CoreFlow, "{\"marker\":\"POISON\"}");
        for (var index = 0; index < Healthy; index++)
        {
            await DeadLetterHost.PublishAsync(consumer, GRConfirmed.LogicalName, DeliveryClass.CoreFlow, "{\"ok\":true}");
        }

        await ChoreographyWorld.WaitUntilAsync(() => Task.FromResult(fault.Successes >= Healthy), _timeout);
        fault.Successes.Should().Be(Healthy, "prefetch=1");
        (await DeadLetterHost.DeadLetterCountAsync(consumer)).Should().Be(1, "poison ke dead_letter, antrian tetap jalan");
    }

    [Fact]
    public async Task Fan_out_sibling_consumer_still_runs_when_another_is_poison()
    {
        // Dua consumer untuk LogicalName dan DeliveryClass yang sama
        var poison = new FaultInjector { Behavior = (_, _) => throw new InvalidOperationException("poison sibling") };
        var healthy = new FaultInjector { Behavior = (_, _) => Result.Success() };
        await using var consumer = await DeadLetterHost.StartFanOutConsumerAsync(
            await fixture.CreateFreshDatabaseAsync("dlq"),
            fixture.RabbitMqConnectionString,
            UniqueName("wms.events"),
            UniqueName("q.dlq"),
            poison,
            healthy,
            GRConfirmed.LogicalName,
            DeliveryClass.CoreFlow);

        await DeadLetterHost.PublishAsync(consumer, GRConfirmed.LogicalName, DeliveryClass.CoreFlow);

        await ChoreographyWorld.WaitUntilAsync(() => Task.FromResult(healthy.Successes >= 1), _timeout);
        healthy.Successes.Should().Be(1, "sibling sehat tetap jalan walau sibling lain poison");
        (await DeadLetterHost.DeadLetterCountAsync(consumer)).Should().Be(1, "sibling poison ke dead_letter, terisolasi");
    }

    [Fact]
    public async Task Producer_publish_failure_dead_letters_after_max_attempts()
    {
        var exchange = UniqueName("wms.events");

        await using var producer = await DeadLetterHost.BuildProducerAsync(
            await fixture.CreateFreshDatabaseAsync("dlq"), "amqp://guest:guest@localhost:1", exchange, UniqueName("q.dlq"));

        await DeadLetterHost.EmitAsync(producer, SampleGrConfirmed(), DeliveryClass.CoreFlow);

        for (var attempt = 0; attempt < 6; attempt++)
        {
            await DeadLetterHost.DrainAsync(producer);
        }

        var rows = await DeadLetterHost.OutboxRowsAsync(producer);
        rows.Should().ContainSingle();
        rows[0].ProcessedAt.Should().NotBeNull("row tak Pending abadi pasca DLQ (fail-loud, tak terloop)");
        (await DeadLetterHost.DeadLetterCountAsync(producer)).Should().Be(1, "1 row dead_letter producer side");
    }

    private static GRConfirmed SampleGrConfirmed() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        [new ReceivedLine("SKU-A", 10m, "B1", null, ReceivedLineStatus.Good)],
        []);

    private static string UniqueName(string prefix) => $"{prefix}.{Guid.NewGuid():N}";

    private async Task<ServiceProvider> StartConsumerAsync(FaultInjector fault) =>
        await DeadLetterHost.StartConsumerAsync(
            await fixture.CreateFreshDatabaseAsync("dlq"),
            fixture.RabbitMqConnectionString,
            UniqueName("wms.events"),
            UniqueName("q.dlq"),
            fault,
            GRConfirmed.LogicalName,
            DeliveryClass.CoreFlow);
}
