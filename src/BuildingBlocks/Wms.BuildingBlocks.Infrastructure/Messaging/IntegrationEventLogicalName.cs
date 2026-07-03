using System.Reflection;

namespace Wms.BuildingBlocks.Infrastructure.Messaging;

// Identitas broker = konvensi 'public const string LogicalName' pada tipe contract.
public static class IntegrationEventLogicalName
{
    public static string Resolve(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        var field = eventType.GetField(
            "LogicalName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (field is null || field.GetValue(null) is not string logicalName)
        {
            throw new InvalidOperationException(
                $"Integration event '{eventType.Name}' wajib punya 'public const string LogicalName'");
        }

        return logicalName;
    }
}
