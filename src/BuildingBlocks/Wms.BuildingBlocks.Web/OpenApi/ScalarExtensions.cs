using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

// Scalar OpenAPI UI dev-only
public static class ScalarExtensions
{
    public static WebApplication MapOpenApiDocumentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();
        app.MapScalarApiReference(options => options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"));

        return app;
    }
}
