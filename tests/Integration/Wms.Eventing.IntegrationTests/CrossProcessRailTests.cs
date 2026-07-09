using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Eventing.IntegrationTests.TestSupport;
using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;
using Wms.Inbound.Infrastructure;
using Wms.Reporting.Persistence;
using Xunit;

namespace Wms.Eventing.IntegrationTests;

// Test eventing rail antar host modul.
[Collection(EventingRailCollection.Name)]
public sealed class CrossProcessRailTests(EventingRailFixture fixture)
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Emitted_event_is_dispatched_over_rabbitmq_and_consumed_with_inbox_and_trace_context()
    {
        var exchange = UniqueName("wms.events");
        await using var producer = EventingRailHost.BuildInboundProducer(
            await fixture.CreateFreshDatabaseAsync("inbound"), fixture.RabbitMqConnectionString, exchange);
        await using var consumer = EventingRailHost.BuildReportingConsumer(
            await fixture.CreateFreshDatabaseAsync("reporting"), fixture.RabbitMqConnectionString, exchange, UniqueName("wms.reporting"));
        await EventingRailHost.MigrateAsync<InboundDbContext>(producer);
        await EventingRailHost.MigrateAsync<ReportingDbContext>(consumer);

        // Pastikan subscriber siap sebelum event dipublish.
        await EventingRailHost.StartSubscriberAsync(consumer);

        // Buat event di dalam activity agar trace context ikut tersimpan.
        using (new Activity("test.emit").SetIdFormat(ActivityIdFormat.W3C).Start())
        {
            await EventingRailHost.EmitToOutboxAsync(producer, SampleGrConfirmed(), DeliveryClass.CoreFlow);
        }

        var emitted = await EventingRailHost.OutboxRowsAsync(producer);
        emitted.Should().ContainSingle();
        var eventId = emitted[0].Id;
        emitted[0].Traceparent.Should().MatchRegex(
            "^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$", "traceparent W3C ter-capture di AddToOutbox");

        // Publish event dari outbox ke broker.
        await EventingRailHost.DrainAsync(producer);

        // Event diproses oleh dua handler Reporting.
        await EventingRailHost.WaitUntilAsync(
            async () => await EventingRailHost.InboxCountAsync(consumer, eventId) >= 2, _timeout);
        (await EventingRailHost.InboxCountAsync(consumer, eventId)).Should().Be(2);

        // Outbox ditandai sudah diproses.
        (await EventingRailHost.OutboxRowsAsync(producer))[0].ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Redelivered_event_is_deduped_by_inbox_effect_once_per_handler()
    {
        var exchange = UniqueName("wms.events");
        await using var consumer = EventingRailHost.BuildReportingConsumer(
            await fixture.CreateFreshDatabaseAsync("reporting"), fixture.RabbitMqConnectionString, exchange, UniqueName("wms.reporting"));
        await EventingRailHost.MigrateAsync<ReportingDbContext>(consumer);
        await EventingRailHost.StartSubscriberAsync(consumer);

        // Kirim envelope yang sama dua kali untuk mensimulasikan redelivery.
        var publisher = consumer.GetRequiredService<IMessagePublisher>();
        var envelope = MessageEnvelope.Create(
            SampleGrConfirmed(), GRConfirmed.LogicalName, DeliveryClass.CoreFlow, Guid.NewGuid(), DateTimeOffset.UtcNow);
        await publisher.PublishAsync(envelope);
        await publisher.PublishAsync(envelope);

        await EventingRailHost.WaitUntilAsync(
            async () => await EventingRailHost.InboxCountAsync(consumer, envelope.EventId) >= 2, _timeout);

        // Tunggu sebentar agar redelivery sempat diproses, lalu pastikan efeknya tetap sekali per handler.
        await Task.Delay(TimeSpan.FromSeconds(1));
        (await EventingRailHost.InboxCountAsync(consumer, envelope.EventId))
            .Should().Be(2, "dua handler Reporting, masing-masing tepat sekali walau event dikirim dua kali");
    }

    private static GRConfirmed SampleGrConfirmed() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        [new ReceivedLine("SKU-A", 10m, "B1", null, ReceivedLineStatus.Good)],
        []);

    private static string UniqueName(string prefix) => $"{prefix}.{Guid.NewGuid():N}";
}
