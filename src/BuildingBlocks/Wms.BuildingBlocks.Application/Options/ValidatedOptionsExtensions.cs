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
}
