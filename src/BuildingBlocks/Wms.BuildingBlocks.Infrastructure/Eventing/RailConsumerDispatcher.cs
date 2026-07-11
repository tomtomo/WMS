using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;

namespace Wms.BuildingBlocks.Infrastructure.Eventing;

// Teruskan envelope ke consumer modul melalui pipeline yang sama untuk worker dan serverless trigger.
public sealed class RailConsumerDispatcher(
    IServiceScopeFactory scopeFactory,
    IEnumerable<RailConsumerRegistration> registrations,
    ILogger<RailConsumerDispatcher> logger)
{
    private readonly IReadOnlyList<RailConsumerRegistration> _registrations = [.. registrations];

    internal IReadOnlyList<RailConsumerRegistration> Registrations => _registrations;

    public async Task<bool> DispatchAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var matches = _registrations
            .Where(registration =>
                registration.DeliveryClass == envelope.DeliveryClass
                && string.Equals(registration.LogicalName, envelope.LogicalName, StringComparison.Ordinal))
            .ToList();

        if (matches.Count == 0)
        {
            // Abaikan message yang tidak punya consumer terdaftar.
            logger.LogWarning(
                "Rail: tak ada consumer untuk {LogicalName}/{DeliveryClass}; ack tanpa proses.",
                envelope.LogicalName,
                envelope.DeliveryClass);
            return true;
        }

        // Jalankan setiap consumer lewat dead letter pipeline.
        foreach (var registration in matches)
        {
            using var deadLetterScope = scopeFactory.CreateScope();
            var pipeline = deadLetterScope.ServiceProvider.GetRequiredService<ConsumerDeadLetterPipeline>();
            try
            {
                await pipeline
                    .ExecuteAsync(
                        envelope.LogicalName,
                        envelope.Payload,
                        async attemptCancellation =>
                        {
                            using var scope = scopeFactory.CreateScope();
                            return await registration
                                .InvokeAsync(scope.ServiceProvider, envelope, attemptCancellation)
                                .ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
#pragma warning disable S2221 // Pastikan kegagalan satu consumer tidak menghentikan consumer lain.
            catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore S2221
            {
                // Log kegagalan pipeline dan lanjutkan consumer berikutnya.
                logger.LogError(
                    exception,
                    "Rail: pipeline consumer {LogicalName}/{DeliveryClass} gagal tak terduga",
                    envelope.LogicalName,
                    envelope.DeliveryClass);
            }
        }

        return true;
    }
}
