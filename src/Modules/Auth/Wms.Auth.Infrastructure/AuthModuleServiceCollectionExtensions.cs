using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Infrastructure;
using Wms.Auth.Infrastructure.Persistence;
using Wms.Auth.Infrastructure.Security;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.Persistence;

namespace Microsoft.Extensions.DependencyInjection;

// Composition modul Auth.
public static class AuthModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "wms")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<AuthDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' untuk modul Auth tidak ditemukan.");

            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", AuthDbContext.Schema))
                .UseSnakeCaseNamingConvention();

            var auditableInterceptor = provider.GetService<AuditableInterceptor>();
            if (auditableInterceptor is not null)
            {
                options.AddInterceptors(auditableInterceptor);
            }
        });

        services.AddScoped<DbContext>(provider => provider.GetRequiredService<AuthDbContext>());

        // Issuer JWT membaca section "Jwt"
        services.AddValidatedOptions<JwtIssuerOptions>(configuration, JwtIssuerOptions.SectionName);

        // Write side.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();

        // Read side — reader EF langsung (bukan Cached).
        services.AddScoped<IEffectivePermissionResolver, EffectivePermissionResolver>();
        services.AddScoped<IUserReader, UserReader>();
        services.AddScoped<IRoleReader, RoleReader>();
        services.AddScoped<IPermissionReader, PermissionReader>();

        // Security
        services.AddScoped<IJwtTokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();

        // Siapkan login Entra dan cache metadata serta signing key agar bisa digunakan ulang dan update otomatis.
        services.AddValidatedOptions<EntraAuthOptions>(configuration, EntraAuthOptions.SectionName);
        services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(provider =>
        {
            var entra = provider.GetRequiredService<IOptions<EntraAuthOptions>>().Value;
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                entra.ResolveMetadataAddress(),
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
        });
        services.AddSingleton<IEntraTokenValidator, EntraTokenValidator>();

        // Adapter status aktif untuk IsActive filter (BuildingBlocks.Web) via IUserReader (null = Disabled).
        services.AddScoped<IActiveUserChecker, ActiveUserChecker>();

        return services;
    }
}
