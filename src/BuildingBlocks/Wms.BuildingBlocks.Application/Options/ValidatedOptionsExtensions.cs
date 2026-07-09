using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

// Registrasi IOptions bervalidasi.
public static class ValidatedOptionsExtensions
{
    public static IServiceCollection AddValidatedOptions<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }

    // Overload untuk konfigurasi yang perlu diberikan langsung, misalnya saat modul belum mendaftarkan IConfiguration ke DI.
    public static IServiceCollection AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
