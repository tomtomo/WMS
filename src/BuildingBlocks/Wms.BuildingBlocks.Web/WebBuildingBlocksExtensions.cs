using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Web;
using Wms.BuildingBlocks.Web.GrpcInterceptors;

namespace Microsoft.Extensions.DependencyInjection;

// Composition kernel Web REST: ProblemDetails dan correlationId, versioning /v{n}/, dokumen OpenAPI, ICurrentUser.
public static class WebBuildingBlocksExtensions
{
    public static IServiceCollection AddWebBuildingBlocks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // CustomizeProblemDetails membungkus error yang digenerate framework (401, 404, 500).
        services.AddProblemDetails(options => options.CustomizeProblemDetails = InjectCorrelationId);

        services.AddApiVersioningDefaults();
        services.AddOpenApiDocumentation();
        services.AddHttpContextCurrentUser();

        return services;
    }

    // Interceptor gRPC server-side untuk host yang expose gRPC internal.
    public static IServiceCollection AddGrpcWebBuildingBlocks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGrpc(options =>
        {
            options.Interceptors.Add<CorrelationIdInterceptor>();
            options.Interceptors.Add<ErrorMappingInterceptor>();
        });

        return services;
    }

    private static void InjectCorrelationId(ProblemDetailsContext context)
    {
        var correlationId = CorrelationId.Get(context.HttpContext);
        if (!string.IsNullOrEmpty(correlationId))
        {
            context.ProblemDetails.Extensions["correlationId"] = correlationId;
        }
    }
}
