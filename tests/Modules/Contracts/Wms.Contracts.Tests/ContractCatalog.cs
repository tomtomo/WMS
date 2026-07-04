using System.Reflection;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Contracts.Tests;

// Tipe kontrak untuk seluruh test
internal static class ContractCatalog
{
    public static readonly IReadOnlyList<Assembly> Assemblies =
    [
        typeof(Wms.Inbound.Contracts.GRConfirmed).Assembly,
        typeof(Wms.Inventory.Contracts.StockAllocationCompleted).Assembly,
        typeof(Wms.Outbound.Contracts.WaveReleased).Assembly,
    ];

    // Integration event = record publik konkret bermarker IIntegrationEvent.
    public static IReadOnlyList<Type> EventTypes =>
        [.. Assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && typeof(IIntegrationEvent).IsAssignableFrom(type))];

    // Semua record kontrak
    public static IReadOnlyList<Type> RecordTypes =>
        [.. Assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                && !type.Name.StartsWith('<'))];
}
