using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Wms.Platform.Azure.Messaging;

// Menyiapkan topic, subscription, dan rule Service Bus untuk rail core flow.
internal static class ServiceBusRailTopology
{
    // Setelah lima kali gagal dikirim, pesan masuk ke DLQ milik subscription.
    internal const int MaxDeliveryCount = 5;

    public static async Task EnsureSubscriptionAsync(
        ServiceBusAdministrationClient administrationClient,
        string topicName,
        string subscriptionName,
        IReadOnlyCollection<string> logicalNames,
        CancellationToken cancellationToken)
    {
        if (!await administrationClient.TopicExistsAsync(topicName, cancellationToken).ConfigureAwait(false))
        {
            await CreateIgnoringRaceAsync(
                () => administrationClient.CreateTopicAsync(topicName, cancellationToken)).ConfigureAwait(false);
        }

        if (!await administrationClient
                .SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken).ConfigureAwait(false))
        {
            var subscription = new CreateSubscriptionOptions(topicName, subscriptionName)
            {
                RequiresSession = true,
                MaxDeliveryCount = MaxDeliveryCount,
            };

            await CreateIgnoringRaceAsync(
                () => administrationClient.CreateSubscriptionAsync(subscription, cancellationToken)).ConfigureAwait(false);
        }

        // Hapus rule default agar subscription hanya menerima logical name yang didaftarkan.
        if (await administrationClient
                .RuleExistsAsync(topicName, subscriptionName, RuleProperties.DefaultRuleName, cancellationToken)
                .ConfigureAwait(false))
        {
            await administrationClient
                .DeleteRuleAsync(topicName, subscriptionName, RuleProperties.DefaultRuleName, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var logicalName in logicalNames)
        {
            if (!await administrationClient
                    .RuleExistsAsync(topicName, subscriptionName, logicalName, cancellationToken).ConfigureAwait(false))
            {
                var rule = new CreateRuleOptions(logicalName, new CorrelationRuleFilter { Subject = logicalName });
                await CreateIgnoringRaceAsync(
                    () => administrationClient.CreateRuleAsync(topicName, subscriptionName, rule, cancellationToken))
                    .ConfigureAwait(false);
            }
        }
    }

    // Bisa ada dua host membuat entity yang sama bersamaan.
    private static async Task CreateIgnoringRaceAsync<TResponse>(Func<Task<TResponse>> createAsync)
    {
        try
        {
            await createAsync().ConfigureAwait(false);
        }
        catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
            // Entity sudah dibuat pihak lain lebih dulu.
        }
    }
}
