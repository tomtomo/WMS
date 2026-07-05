using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wms.MasterData.Application.Features.Location.CreateLocation;
using Wms.MasterData.Application.Features.Product.CreateProduct;
using Wms.MasterData.Application.Features.Warehouse.CreateWarehouse;
using Wms.MasterData.Application.Features.Warehouse.DeactivateWarehouse;

namespace Wms.MasterData.IntegrationTests.TestSupport;

// Seed lewat pipeline
internal static class MasterDataScenarios
{
    public static async Task<Guid> CreateWarehouseAsync(
        IServiceProvider provider,
        string name = "DC Jakarta Cakung",
        string address = "Jl. Raya Cakung No. 1")
    {
        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateWarehouseCommand(name, address));
        return result.Value;
    }

    public static async Task DeactivateWarehouseAsync(IServiceProvider provider, Guid warehouseId)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new DeactivateWarehouseCommand(warehouseId));
    }

    public static async Task<Guid> CreateLocationAsync(
        IServiceProvider provider,
        Guid warehouseId,
        string type = "Rack",
        string code = "RACK-B12-03")
    {
        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateLocationCommand(warehouseId, type, code));
        return result.Value;
    }

    public static async Task<string> CreateProductAsync(
        IServiceProvider provider,
        string sku = "SKU-MILK",
        string name = "Fresh Milk 1L",
        string uom = "carton",
        bool batchTracking = true,
        bool expiryTracking = true,
        bool qcRequired = false,
        int? shelfLifeDays = 30)
    {
        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateProductCommand(sku, name, uom, batchTracking, expiryTracking, qcRequired, shelfLifeDays));
        return result.Value;
    }
}
