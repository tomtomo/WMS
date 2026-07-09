using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Web;

namespace Wms.WebUI.Bff;

// Tambahkan bearer token dan correlation id ke setiap request menuju gateway.
// Token diambil dari token store server-side berdasarkan session id di cookie.
internal sealed class BearerForwardingHandler(IHttpContextAccessor httpContextAccessor, ITokenStore tokenStore)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var sessionId = httpContext.User.FindFirst(BffClaims.SessionId)?.Value;
            if (sessionId is not null && tokenStore.Get(sessionId) is { } accessToken)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            var correlationId = CorrelationId.Get(httpContext);
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, correlationId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
