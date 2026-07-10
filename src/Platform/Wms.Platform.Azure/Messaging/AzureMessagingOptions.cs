using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Azure.Messaging;

// Konfigurasi messaging untuk platform Azure.
public sealed class AzureMessagingOptions
{
    public const string SectionName = "AzurePlatform:Messaging";

    [Required]
    public string ServiceBusConnectionStringName { get; set; } = "servicebus";

    // Topic untuk core flow antar modul. Tiap host modul bisa punya subscription sendiri.
    [Required]
    public string CoreFlowTopicName { get; set; } = "wms-core-flow";

    // Queue untuk delayed task, memakai namespace Service Bus yang sama.
    [Required]
    public string DelayedTaskQueueName { get; set; } = "wms-delayed-tasks";

    // Endpoint dan key Event Grid sengaja tanpa default, supaya konfigurasi yang belum lengkap langsung diketahui saat startup.
    [Required]
    public string EventGridTopicEndpoint { get; set; } = string.Empty;

    [Required]
    public string EventGridTopicKey { get; set; } = string.Empty;

    [Required]
    public string EventHubsConnectionStringName { get; set; } = "eventhubs";
}
