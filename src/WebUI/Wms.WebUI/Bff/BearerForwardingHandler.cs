using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Web;

namespace Wms.WebUI.Bff;

// Tambahkan bearer token dan correlation id ke setiap request menuju gateway.
// Token dari token store server side by session id. Static SSR: HttpContext. circuit interaktif: auth state circuit.
internal sealed class BearerForwardingHandler(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    ITokenStore tokenStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        // Jika HttpContext tidak tersedia di circuit Blazor, ambil pengguna dari authentication state.
        if (user is null && circuitServicesAccessor.Services is { } circuitServices)
        {
            var authProvider = circuitServices.GetRequiredService<AuthenticationStateProvider>();
            user = (await authProvider.GetAuthenticationStateAsync()).User;
        }

        var sessionId = user?.FindFirst(BffClaims.SessionId)?.Value;
        if (sessionId is not null && tokenStore.Get(sessionId) is { } accessToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (httpContext is not null)
        {
            var correlationId = CorrelationId.Get(httpContext);
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, correlationId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
