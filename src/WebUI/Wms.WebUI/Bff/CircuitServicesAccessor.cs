using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Wms.WebUI.Bff;

// Sediakan akses ke service dalam circuit Blazor dari handler yang berjalan di luar circuit.
internal sealed class CircuitServicesAccessor
{
    private static readonly AsyncLocal<IServiceProvider?> _services = new();

    [SuppressMessage(
        "Minor Code Smell",
        "S2325:Methods and properties that don't access instance data should be static",
        Justification = "Instance property yang sengaja back ke AsyncLocal statik — pola resmi Blazor CircuitServicesAccessor.")]
    public IServiceProvider? Services
    {
        get => _services.Value;
        set => _services.Value = value;
    }
}

// Sediakan service circuit selama proses render dan event agar handler lain dapat mengaksesnya.
internal sealed class ServicesAccessorCircuitHandler(
    IServiceProvider services,
    CircuitServicesAccessor accessor) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next) =>
        async context =>
        {
            accessor.Services = services;
            await next(context);
            accessor.Services = null;
        };
}
