using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Web;

// Honor 'Idempotency-Key'
public sealed class IdempotencyKeyEndpointFilter(IApiIdempotencyStore store) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";

    private static readonly TimeSpan _timeToLive = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.HttpContext.Request;
        if (!request.Headers.TryGetValue(HeaderName, out var header) || string.IsNullOrWhiteSpace(header))
        {
            return await next(context);
        }

        // Key di scope per endpoint (method dan path) — key sama di endpoint berbeda tidak saling replay.
        var storeKey = $"{request.Method}:{request.Path}:{header}";
        var cached = await store.GetResponseAsync(storeKey, context.HttpContext.RequestAborted);
        if (cached is not null)
        {
            var replay = JsonSerializer.Deserialize<StoredResponse>(cached, _serializerOptions);
            if (replay is not null)
            {
                return replay.Body is null
                    ? Results.StatusCode(replay.StatusCode)
                    : Results.Content(replay.Body, "application/json; charset=utf-8", statusCode: replay.StatusCode);
            }
        }

        var result = await next(context);

        // Hanya sukses yang disimpan — kegagalan bisnis boleh beda saat retry.
        var statusCode = (result as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status200OK;
        if (statusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices)
        {
            var value = (result as IValueHttpResult)?.Value;
            var body = value is null ? null : JsonSerializer.Serialize(value, _serializerOptions);
            var stored = JsonSerializer.Serialize(new StoredResponse(statusCode, body), _serializerOptions);

            // Penyimpanan kuitansi tidak boleh batal karena klien disconnect.
            await store.SaveResponseAsync(storeKey, stored, _timeToLive, CancellationToken.None);
        }

        return result;
    }

    private sealed record StoredResponse(int StatusCode, string? Body);
}
