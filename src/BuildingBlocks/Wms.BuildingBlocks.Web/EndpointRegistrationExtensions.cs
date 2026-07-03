using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;

namespace Microsoft.AspNetCore.Builder;

// Host cukup panggil app.MapEndpoints(assembly) sekali per modul.
public static class EndpointRegistrationExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var map in DiscoverMapDelegates(assembly))
        {
            map(app);
        }

        return app;
    }

    // static abstract MapEndpoint
    private static IEnumerable<Action<IEndpointRouteBuilder>> DiscoverMapDelegates(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsAbstract: false, IsInterface: false } || !type.IsAssignableTo(typeof(IEndpoint)))
            {
                continue;
            }

            var method = type.GetMethod(
                nameof(IEndpoint.MapEndpoint),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(IEndpointRouteBuilder)],
                modifiers: null);

            if (method is not null)
            {
                yield return method.CreateDelegate<Action<IEndpointRouteBuilder>>();
            }
        }
    }
}
