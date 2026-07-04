using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Web;

namespace Microsoft.AspNetCore.Builder;

// Pasang Idempotency-Key: .WithIdempotencyKey() setelah MapPost.
public static class IdempotencyKeyEndpointExtensions
{
    public static RouteHandlerBuilder WithIdempotencyKey(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter<IdempotencyKeyEndpointFilter>();
    }
}
