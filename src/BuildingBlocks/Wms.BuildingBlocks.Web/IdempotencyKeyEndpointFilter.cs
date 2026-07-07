using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Web;

// Menangani Idempotency Key agar request duplikat tidak diproses ulang.
public sealed class IdempotencyKeyEndpointFilter(IApiIdempotencyStore store) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";

    private static readonly TimeSpan _timeToLive = TimeSpan.FromHours(24);

    // Pending claim otomatis kedaluwarsa agar key tidak terkunci jika proses berhenti.
    private static readonly TimeSpan _pendingTimeToLive = TimeSpan.FromMinutes(1);

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly Error _inProgressError = new(
        "idempotency.in_progress",
        "Request dengan Idempotency-Key sama sedang diproses; tunggu sampai selesai lalu coba lagi.");

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.HttpContext.Request;
        if (!request.Headers.TryGetValue(HeaderName, out var header) || string.IsNullOrWhiteSpace(header))
        {
            return await next(context);
        }

        // Key yang sama tetap dianggap berbeda jika endpointnya berbeda.
        var storeKey = $"{request.Method}:{request.Path}:{header}";
        var reservation = await store.TryReserveAsync(storeKey, _pendingTimeToLive, context.HttpContext.RequestAborted);
        if (reservation is { Status: IdempotencyReservationStatus.Completed, StoredResponse: not null })
        {
            var replay = JsonSerializer.Deserialize<StoredResponse>(reservation.StoredResponse, _serializerOptions);
            if (replay is not null)
            {
                return replay.Body is null
                    ? Results.StatusCode(replay.StatusCode)
                    : Results.Content(replay.Body, "application/json; charset=utf-8", statusCode: replay.StatusCode);
            }
        }

        if (reservation.Status == IdempotencyReservationStatus.Pending)
        {
            var problem = ProblemDetailsMapper.ToProblemDetails(
                ResultErrorType.Conflict,
                _inProgressError,
                CorrelationId.Get(context.HttpContext));
            return Results.Problem(problem);
        }

        object? result;
        try
        {
            result = await next(context);
        }
        catch
        {
            // Lepas klaim agar request berikutnya bisa retry
            await ReleaseQuietlyAsync(storeKey, reservation.OwnerToken);
            throw;
        }

        // Hanya sukses yang disimpan, kegagalan bisnis boleh beda saat retry.
        var statusCode = (result as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status200OK;
        if (statusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices)
        {
            var value = (result as IValueHttpResult)?.Value;
            var body = value is null ? null : JsonSerializer.Serialize(value, _serializerOptions);
            var stored = JsonSerializer.Serialize(new StoredResponse(statusCode, body), _serializerOptions);

            // Tetap simpan hasil meski request klien sudah putus.
            await store.CompleteAsync(storeKey, reservation.OwnerToken, stored, _timeToLive, CancellationToken.None);
        }
        else
        {
            await ReleaseQuietlyAsync(storeKey, reservation.OwnerToken);
        }

        return result;
    }

    // Gagal release tidak boleh menimpa hasil request asli.
    private async Task ReleaseQuietlyAsync(string storeKey, Guid ownerToken)
    {
        try
        {
            await store.ReleaseAsync(storeKey, ownerToken, CancellationToken.None);
        }
#pragma warning disable RCS1075
        catch (Exception)
        {
            // Abaikan, klaim pending akan kedaluwarsa sendiri.
        }
#pragma warning restore RCS1075
    }

    private sealed record StoredResponse(int StatusCode, string? Body);
}
