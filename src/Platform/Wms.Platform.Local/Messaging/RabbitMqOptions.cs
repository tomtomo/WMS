using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Local.Messaging;

// Nama connection-string mengikuti resource AppHost. Section "LocalPlatform:RabbitMq".
public sealed class RabbitMqOptions
{
    public const string SectionName = "LocalPlatform:RabbitMq";

    [Required]
    public string ConnectionStringName { get; set; } = "rabbitmq";

    [Required]
    public string ExchangeName { get; set; } = "wms.events";

    // Queue subscriber = "{prefix}.{logicalName}", prefix per service/host.
    [Required]
    public string SubscriberQueuePrefix { get; set; } = "wms";

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan PublisherConfirmTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
