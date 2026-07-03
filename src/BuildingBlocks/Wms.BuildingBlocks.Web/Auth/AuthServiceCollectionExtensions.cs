using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web.Auth;

namespace Microsoft.Extensions.DependencyInjection;

// DI auth host REST: validasi JWT dan populate ICurrentUser.
public static class AuthServiceCollectionExtensions
{
    // ICurrentUser dari HttpContext (SYSTEM fallback)
    public static IServiceCollection AddHttpContextCurrentUser(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }

    // JWT RS256, validate offline pakai public key.
    public static IServiceCollection AddJwtBearerRs256(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtBearerRs256Options>()
            .Bind(configuration.GetSection(JwtBearerRs256Options.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Isi TokenValidationParameters dari options tervalidasi (rs256.Value throw bila key kosong).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtBearerRs256Options>>((bearer, rs256) =>
            {
                bearer.TokenValidationParameters = JwtBearerSetup.BuildValidationParameters(rs256.Value);

                // false = pertahankan nama klaim JWT asli.
                bearer.MapInboundClaims = false;
            });

        return services;
    }
}
