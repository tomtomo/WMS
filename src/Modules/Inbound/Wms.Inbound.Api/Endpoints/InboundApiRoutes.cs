using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Wms.Inbound.Api.Endpoints;

// Satu group /v{n}/goods-receipts
internal static class InboundApiRoutes
{
    public static RouteGroupBuilder GoodsReceipts(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet("goods-receipts")
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        return app.MapGroup("/v{version:apiVersion}/goods-receipts")
            .WithApiVersionSet(versionSet)
            .WithTags("GoodsReceipts");
    }
}
