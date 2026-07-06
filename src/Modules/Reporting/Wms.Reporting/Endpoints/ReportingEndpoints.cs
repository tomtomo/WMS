using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Reporting.Abstractions;

namespace Wms.Reporting.Endpoints;

// REST Report : Stock on Hand, Supplier Performance, Dispatch Summary, Operator Productivity.
public sealed class ReportingEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = ReportingApiRoutes.Reports(app);
        group.MapGet("/stock-on-hand", StockOnHandAsync).WithName("GetStockOnHand");
        group.MapGet("/supplier-performance", SupplierPerformanceAsync).WithName("GetSupplierPerformance");
        group.MapGet("/dispatch-summary", DispatchSummaryAsync).WithName("GetDispatchSummary");
        group.MapGet("/operator-productivity", OperatorProductivityAsync).WithName("GetOperatorProductivity");
    }

    private static async Task<IResult> StockOnHandAsync(
        IStockOnHandReader reader,
        CancellationToken cancellationToken,
        Guid? warehouseId = null,
        string? sku = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await reader.ListAsync(warehouseId, sku, page, pageSize, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SupplierPerformanceAsync(
        IReceivingSummaryReader reader,
        CancellationToken cancellationToken,
        Guid? supplierId = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await reader.ListAsync(supplierId, page, pageSize, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DispatchSummaryAsync(
        IDispatchSummaryReader reader,
        CancellationToken cancellationToken,
        Guid? warehouseId = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await reader.ListAsync(warehouseId, page, pageSize, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> OperatorProductivityAsync(
        IOperatorActivityReader reader,
        CancellationToken cancellationToken,
        Guid? operatorId = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await reader.ListAsync(operatorId, page, pageSize, cancellationToken);
        return Results.Ok(result);
    }
}
