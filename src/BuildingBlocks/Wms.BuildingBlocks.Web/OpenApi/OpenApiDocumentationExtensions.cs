using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

// Generator dokumen OpenAPI
public static class OpenApiDocumentationExtensions
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Satu SwaggerDoc per versi
        services.AddOptions<SwaggerGenOptions>()
            .Configure<IApiVersionDescriptionProvider>((swagger, provider) =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    swagger.SwaggerDoc(
                        description.GroupName,
                        new OpenApiInfo { Title = "TomSandboxWMS API", Version = description.ApiVersion.ToString() });
                }
            });

        return services;
    }
}
