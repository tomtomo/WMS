using AwesomeAssertions;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using NSubstitute;
using Wms.Platform.Azure.Saga;
using Wms.Platform.Azure.Saga.Orchestrations;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Kontrak saga Azure: start via Durable Task client
public sealed class SagaParityTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("parity-test");
    private readonly TaskOrchestrationContext _context = Substitute.For<TaskOrchestrationContext>();

    [Fact]
    public async Task Start_schedules_an_orchestration_named_after_the_saga_data_type()
    {
        _client.GetInstanceAsync("wave-cancel-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<OrchestrationMetadata?>(null));
        var orchestrator = new DurableFunctionsSagaOrchestrator(_client);
        var data = new WaveCancelSagaData("W1");

        await orchestrator.StartAsync("wave-cancel-1", data);

        await _client.Received(1).ScheduleNewOrchestrationInstanceAsync(
            Arg.Is<TaskName>(name => name.Name == nameof(WaveCancelSagaData)),
            data,
            Arg.Is<StartOrchestrationOptions>(options => options.InstanceId == "wave-cancel-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_start_of_a_live_saga_is_rejected()
    {
        _client.GetInstanceAsync("wave-cancel-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<OrchestrationMetadata?>(
                new OrchestrationMetadata(nameof(WaveCancelSagaData), "wave-cancel-1")
                {
                    RuntimeStatus = OrchestrationRuntimeStatus.Running,
                }));
        var orchestrator = new DurableFunctionsSagaOrchestrator(_client);

        var start = async () => await orchestrator.StartAsync("wave-cancel-1", new WaveCancelSagaData("W1"));

        await start.Should().ThrowAsync<InvalidOperationException>("parity InProc: start ganda saga hidup ditolak");
    }

    [Fact]
    public async Task Completed_prior_instance_does_not_block_a_new_start()
    {
        _client.GetInstanceAsync("wave-cancel-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<OrchestrationMetadata?>(
                new OrchestrationMetadata(nameof(WaveCancelSagaData), "wave-cancel-1")
                {
                    RuntimeStatus = OrchestrationRuntimeStatus.Completed,
                }));
        var orchestrator = new DurableFunctionsSagaOrchestrator(_client);

        await orchestrator.StartAsync("wave-cancel-1", new WaveCancelSagaData("W1"));

        await _client.ReceivedWithAnyArgs(1).ScheduleNewOrchestrationInstanceAsync(default, default(object), default, default);
    }

    [Fact]
    public async Task Saga_runs_every_step_in_order_when_all_succeed()
    {
        var calls = RecordActivityCalls();
        var definition = new SagaDefinition(
            [new SagaStep("CancelWave", "ReopenWave"), new SagaStep("ReleaseReservation", "ReReserve")],
            """{"waveId":"W1"}""");

        var outcome = await new SagaOrchestration().RunAsync(_context, definition);

        outcome.Should().Be(SagaOutcome.Completed());
        calls.Should().Equal("CancelWave", "ReleaseReservation");
    }

    [Fact]
    public async Task Failure_mid_saga_compensates_completed_steps_in_reverse_order()
    {
        var calls = RecordActivityCalls("ShipConfirm");
        var definition = new SagaDefinition(
            [
                new SagaStep("CancelWave", "ReopenWave"),
                new SagaStep("ReleaseReservation", "ReReserve"),
                new SagaStep("ShipConfirm", "UnshipConfirm"),
                new SagaStep("NeverReached", "NeverCompensated"),
            ],
            """{"waveId":"W1"}""");

        var outcome = await new SagaOrchestration().RunAsync(_context, definition);

        outcome.Should().Be(SagaOutcome.Compensated("ShipConfirm"));
        calls.Should().Equal(
            "CancelWave", "ReleaseReservation", "ShipConfirm", "ReReserve", "ReopenWave");
    }

    [Fact]
    public async Task Step_without_compensation_is_skipped_during_rollback()
    {
        var calls = RecordActivityCalls("StepC");
        var definition = new SagaDefinition(
            [new SagaStep("StepA"), new SagaStep("StepB", "UndoB"), new SagaStep("StepC")],
            "{}");

        var outcome = await new SagaOrchestration().RunAsync(_context, definition);

        outcome.Should().Be(SagaOutcome.Compensated("StepC"));
        calls.Should().Equal("StepA", "StepB", "StepC", "UndoB");
    }

    [Fact]
    public async Task Compensation_failure_does_not_stop_the_remaining_rollback()
    {
        var calls = RecordActivityCalls("StepC", "UndoB");
        var definition = new SagaDefinition(
            [new SagaStep("StepA", "UndoA"), new SagaStep("StepB", "UndoB"), new SagaStep("StepC", "UndoC")],
            "{}");

        var outcome = await new SagaOrchestration().RunAsync(_context, definition);

        // Kompensasi tidak tuntas harus terbedakan dari rollback bersih, dan UndoA tetap dijalankan.
        outcome.Should().Be(SagaOutcome.CompensationIncomplete("StepC", "UndoB"));
        calls.Should().Equal("StepA", "StepB", "StepC", "UndoB", "UndoA");
    }

    private List<string> RecordActivityCalls(params string[] failOn)
    {
        var calls = new List<string>();
        _context.CallActivityAsync<object?>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(callInfo =>
            {
                var name = callInfo.Arg<TaskName>().Name;
                calls.Add(name);
                return failOn.Contains(name, StringComparer.Ordinal)
                    ? Task.FromException<object?>(new InvalidOperationException($"activity {name} gagal"))
                    : Task.FromResult<object?>(null);
            });
        return calls;
    }

    private sealed record WaveCancelSagaData(string WaveId);
}
