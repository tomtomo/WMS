namespace Microsoft.AspNetCore.Builder;

// Pipeline kernel Web
public static class WebBuildingBlocksApplicationExtensions
{
    public static WebApplication UseWebBuildingBlocks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseCorrelationId();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapOpenApiDocumentation();

        return app;
    }
}
