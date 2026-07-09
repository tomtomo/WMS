using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using CorrelationIdHelper = Wms.BuildingBlocks.Web.CorrelationId;

namespace Wms.BuildingBlocks.Web;

// ICorrelationContext dari HttpContext.Items (di set CorrelationIdMiddleware).
public sealed class HttpContextCorrelationContext(IHttpContextAccessor httpContextAccessor) : ICorrelationContext
{
    public string? CorrelationId =>
        httpContextAccessor.HttpContext is { } httpContext ? CorrelationIdHelper.Get(httpContext) : null;
}
