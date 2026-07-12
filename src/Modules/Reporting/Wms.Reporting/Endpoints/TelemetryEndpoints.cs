using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Endpoints;

// Endpoint REST untuk menampilkan ringkasan telemetry operasional terbaru per gudang.
// Endpoint ini digunakan oleh WebUI dan dilindungi oleh autentikasi host.
public sealed class TelemetryEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = ReportingApiRoutes.Telemetry(app);
        group.MapGet("/summary", SummaryAsync).WithName("GetOperationalTelemetrySummary");
    }

    private static async Task<IResult> SummaryAsync(
        IOperationalTelemetryStore store,
        IFeatureManager featureManager,
        Guid warehouseId,
        CancellationToken cancellationToken,
        int windowMinutes = 60)
    {
        if (!await featureManager.IsEnabledAsync(ReportingFeatureFlags.TelemetrySummary))
        {
            return Results.NotFound();
        }

        var window = TimeSpan.FromMinutes(Math.Max(1, windowMinutes));
        var records = await store.GetRecentAsync(warehouseId, window, cancellationToken);
        return Results.Ok(TelemetrySummary.Build(warehouseId, records));
    }
}
