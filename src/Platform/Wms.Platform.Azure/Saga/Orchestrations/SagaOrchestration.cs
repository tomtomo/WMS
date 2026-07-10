using Microsoft.DurableTask;

namespace Wms.Platform.Azure.Saga.Orchestrations;

// Orchestrator saga generik: jalankan langkah satu per satu, kalau ada yang gagal, batalkan langkah yang sudah selesai dari belakang.
public sealed class SagaOrchestration : TaskOrchestrator<SagaDefinition, SagaOutcome>
{
    public override async Task<SagaOutcome> RunAsync(TaskOrchestrationContext context, SagaDefinition input)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        var completed = new List<SagaStep>();
        foreach (var step in input.Steps)
        {
            try
            {
                await context.CallActivityAsync<object?>(new TaskName(step.ActivityName), input.PayloadJson);
            }
#pragma warning disable S2221 // Kalau activity gagal, saga harus masuk kompensasi dulu sebelum berhenti.
            catch (Exception)
#pragma warning restore S2221
            {
                var compensations = Enumerable.Reverse(completed)
                    .Where(done => done.CompensationActivityName is not null);
                foreach (var done in compensations)
                {
                    await context.CallActivityAsync<object?>(new TaskName(done.CompensationActivityName!), input.PayloadJson);
                }

                return SagaOutcome.Compensated(step.ActivityName);
            }

            completed.Add(step);
        }

        return SagaOutcome.Completed();
    }
}

// Satu langkah saga: activity utama, plus activity pembatalan kalau dibutuhkan.
public sealed record SagaStep(string ActivityName, string? CompensationActivityName = null);

// Definisi saga berisi urutan langkah dan payload JSON yang dikirim ke setiap activity.
public sealed record SagaDefinition(IReadOnlyList<SagaStep> Steps, string PayloadJson);

// Hasil akhir saga: selesai semua, atau sudah dikompensasi setelah satu activity gagal.
public sealed record SagaOutcome(bool IsCompensated, string? FailedActivityName)
{
    public static SagaOutcome Completed() => new(false, null);

    public static SagaOutcome Compensated(string failedActivityName) => new(true, failedActivityName);
}
