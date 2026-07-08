using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        services.Configure<JwtIssuerOptions>(configuration.GetSection(JwtIssuerOptions.SectionName));

        // Write side.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Read side — reader EF langsung (bukan Cached).
        services.AddScoped<IEffectivePermissionResolver, EffectivePermissionResolver>();
        services.AddScoped<IUserReader, UserReader>();
        services.AddScoped<IRoleReader, RoleReader>();
        services.AddScoped<IPermissionReader, PermissionReader>();

        // Security
        services.AddScoped<IJwtTokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();

        // Adapter status aktif untuk IsActive filter (BuildingBlocks.Web) via IUserReader (null = Disabled).
        services.AddScoped<IActiveUserChecker, ActiveUserChecker>();

        return services;
    }
}
