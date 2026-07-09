using Wms.Contracts.Abstractions;

namespace Wms.Platform.Local.Messaging;

// Membuat routing key RabbitMQ berdasarkan delivery class dan logical name.
internal static class RabbitMqRouting
{
    public static string RoutingKey(DeliveryClass deliveryClass, string logicalName) =>
        $"{ClassToken(deliveryClass)}.{logicalName}";

    private static string ClassToken(DeliveryClass deliveryClass) => deliveryClass switch
    {
        DeliveryClass.CoreFlow => "core-flow",
        DeliveryClass.Notification => "notification",
        _ => throw new ArgumentOutOfRangeException(nameof(deliveryClass), deliveryClass, "DeliveryClass tak dikenal."),
    };
}
