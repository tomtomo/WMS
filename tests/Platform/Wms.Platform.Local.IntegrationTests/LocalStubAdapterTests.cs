using AwesomeAssertions;
using Wms.Platform.Local.Analytics;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.Notifications;
using Wms.Platform.Local.Saga;
using Wms.Platform.Local.Security;
using Wms.Platform.Local.Streaming;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

// Adapter stub Local: notif log-stub (synthetic providerMessageId), trust-stub s2s, saga shell, stream ring + analytics dev
public sealed class LocalStubAdapterTests
{
    [Fact]
    public async Task Email_stub_logs_synthetic_provider_message_id()
    {
        var logger = new CapturingLogger<LoggingEmailSender>();
        var sender = new LoggingEmailSender(logger);

        await sender.SendAsync("spv@wms.local", "GR pending review", "GR-7 menunggu keputusan");

        logger.StateValue("ProviderMessageId").Should().NotBeNullOrWhiteSpace();
        logger.Messages.Should().ContainSingle();
    }

    [Fact]
    public async Task Push_stub_logs_synthetic_provider_message_id()
    {
        var logger = new CapturingLogger<LoggingPushNotifier>();
        var notifier = new LoggingPushNotifier(logger);

        await notifier.PushAsync("device-77", "Putaway task", "PT-9 ditugaskan");

        logger.StateValue("ProviderMessageId").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task In_app_stub_logs_synthetic_provider_message_id()
    {
        var logger = new CapturingLogger<LoggingInAppNotifier>();
        var notifier = new LoggingInAppNotifier(logger);

        await notifier.NotifyAsync("user-77", "Wave siap dirilis");

        logger.StateValue("ProviderMessageId").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Trust_stub_returns_non_null_synthetic_token()
    {
        var provider = new TrustStubServiceTokenProvider();

        var token = await provider.GetTokenAsync("masterdata-grpc");

        token.Should().NotBeNullOrWhiteSpace();
        token.Should().Contain("masterdata-grpc");
    }

    [Fact]
    public async Task Saga_start_transitions_to_started_state()
    {
        var orchestrator = new InProcSagaOrchestrator(new CapturingLogger<InProcSagaOrchestrator>());

        await orchestrator.StartAsync("wave-cancel-9", new { WaveId = 9 });

        orchestrator.TryGetStatus("wave-cancel-9", out var status).Should().BeTrue();
        status.Should().Be(InProcSagaOrchestrator.StartedStatus);
    }

    [Fact]
    public async Task Saga_duplicate_start_is_rejected()
    {
        var orchestrator = new InProcSagaOrchestrator(new CapturingLogger<InProcSagaOrchestrator>());
        await orchestrator.StartAsync("wave-cancel-9", new { WaveId = 9 });

        var act = () => orchestrator.StartAsync("wave-cancel-9", new { WaveId = 9 });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Stream_publish_then_consume_round_trips_payload()
    {
        var ring = new InProcStreamRing();
        var publisher = new InProcStreamPublisher(ring);
        var consumer = new InProcStreamConsumer(ring);
        var received = new List<PingPayload>();

        await publisher.PublishAsync("scan-telemetry", new PingPayload("scan-1"));
        await consumer.ConsumeAsync<PingPayload>("scan-telemetry", (payload, _) =>
        {
            received.Add(payload);
            return Task.CompletedTask;
        });

        received.Should().ContainSingle().Which.Message.Should().Be("scan-1");
    }

    [Fact]
    public async Task Analytics_sink_emits_flat_log_line()
    {
        var logger = new CapturingLogger<LogCsvAnalyticsSink>();
        var sink = new LogCsvAnalyticsSink(logger);

        await sink.EmitAsync(new PingPayload("olap-row"));

        logger.Messages.Should().ContainSingle().Which.Should().Contain(nameof(PingPayload));
    }
}
