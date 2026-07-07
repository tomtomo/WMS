using System.Collections.Concurrent;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

// Filter Idempotency Key
public sealed class IdempotencyKeyEndpointFilterTests
{
    private readonly RecordingIdempotencyStore _store = new();

    [Fact]
    public async Task Tanpa_header_pass_through_tanpa_menyentuh_store()
    {
        var filter = new IdempotencyKeyEndpointFilter(_store);
        var executed = 0;

        var result = await filter.InvokeAsync(
            Context("POST", "/v1/things", idempotencyKey: null),
            _ =>
            {
                executed++;
                return ValueTask.FromResult<object?>(Results.Ok(new { id = 1 }));
            });

        executed.Should().Be(1);
        _store.Saved.Should().BeEmpty();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Miss_eksekusi_lalu_simpan_respons_2xx()
    {
        var filter = new IdempotencyKeyEndpointFilter(_store);

        await filter.InvokeAsync(
            Context("POST", "/v1/things", "key-1"),
            _ => ValueTask.FromResult<object?>(Results.Created("/v1/things/9", new { id = 9 })));

        _store.Saved.Should().ContainSingle();
        _store.Saved.Keys.Single().Should().Be("POST:/v1/things:key-1", "key di-scope per endpoint");

        // Respons tersimpan = envelope {statusCode, body} dengan body JSON sebagai string.
        using var stored = System.Text.Json.JsonDocument.Parse(_store.Saved.Values.Single());
        stored.RootElement.GetProperty("statusCode").GetInt32().Should().Be(StatusCodes.Status201Created);
        stored.RootElement.GetProperty("body").GetString().Should().Contain("\"id\":9");
    }

    [Fact]
    public async Task Hit_replay_tanpa_eksekusi_handler()
    {
        var filter = new IdempotencyKeyEndpointFilter(_store);
        await filter.InvokeAsync(
            Context("POST", "/v1/things", "key-1"),
            _ => ValueTask.FromResult<object?>(Results.Created("/v1/things/9", new { id = 9 })));

        var executed = 0;
        var replayed = await filter.InvokeAsync(
            Context("POST", "/v1/things", "key-1"),
            _ =>
            {
                executed++;
                return ValueTask.FromResult<object?>(Results.Created("/v1/things/10", new { id = 10 }));
            });

        executed.Should().Be(0, "HIT harus short-circuit");
        var statusCode = (replayed as IStatusCodeHttpResult)!.StatusCode;
        statusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Respons_gagal_tidak_disimpan()
    {
        var filter = new IdempotencyKeyEndpointFilter(_store);

        await filter.InvokeAsync(
            Context("POST", "/v1/things", "key-err"),
            _ => ValueTask.FromResult<object?>(Results.Problem(statusCode: StatusCodes.Status409Conflict)));

        _store.Saved.Should().BeEmpty("retry kegagalan bisnis boleh menghasilkan keputusan berbeda");
    }

    [Fact]
    public async Task Duplikat_konkuren_hanya_satu_eksekusi_handler()
    {
        var filter = new IdempotencyKeyEndpointFilter(_store);
        var executed = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async ValueTask<object?> SlowHandlerAsync()
        {
            Interlocked.Increment(ref executed);

            // Tahan sebentar agar kedua request overlap.
            await gate.Task;
            return Results.Created("/v1/things/9", new { id = 9 });
        }

        async Task<object?> RunRequestAsync() =>
            await filter.InvokeAsync(Context("POST", "/v1/things", "key-race"), _ => SlowHandlerAsync());

        var first = RunRequestAsync();
        var second = RunRequestAsync();
        gate.SetResult();
        var results = await Task.WhenAll(first, second);

        executed.Should().Be(1, "hanya pemenang reservasi yang boleh mengeksekusi side-effect");

        var statusCodes = results
            .Select(result => (result as IStatusCodeHttpResult)?.StatusCode)
            .OrderBy(status => status)
            .ToArray();
        statusCodes.Should().Contain(StatusCodes.Status201Created);
        statusCodes.Should().Contain(
            StatusCodes.Status409Conflict,
            "yang kalah reservasi (masih pending) harus ditolak 409, bukan ikut eksekusi");
    }

    [Fact]
    public async Task Handler_meledak_klaim_dilepas_agar_retry_bisa_eksekusi()
    {
        var filter = new IdempotencyKeyEndpointFilter(_store);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(
                Context("POST", "/v1/things", "key-boom"),
                _ => throw new InvalidOperationException("meledak")));

        var executed = 0;
        var retried = await filter.InvokeAsync(
            Context("POST", "/v1/things", "key-boom"),
            _ =>
            {
                executed++;
                return ValueTask.FromResult<object?>(Results.Created("/v1/things/9", new { id = 9 }));
            });

        executed.Should().Be(1, "klaim pending harus dilepas saat handler melempar");
        (retried as IStatusCodeHttpResult)!.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task Release_gagal_tidak_menutupi_exception_handler_asli()
    {
        var filter = new IdempotencyKeyEndpointFilter(new ReleaseThrowsStore());

        // Exception handler asli yang harus sampai ke caller, bukan kegagalan Release.
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(
                Context("POST", "/v1/things", "key-release-boom"),
                _ => throw new InvalidOperationException("handler asli meledak")));
    }

    [Fact]
    public async Task Replay_204_tanpa_body()
    {
        var filter = new IdempotencyKeyEndpointFilter(_store);
        await filter.InvokeAsync(
            Context("POST", "/v1/things/1/confirm", "key-2"),
            _ => ValueTask.FromResult<object?>(Results.NoContent()));

        var replayed = await filter.InvokeAsync(
            Context("POST", "/v1/things/1/confirm", "key-2"),
            _ => ValueTask.FromResult<object?>(Results.NoContent()));

        (replayed as IStatusCodeHttpResult)!.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    private static TestEndpointFilterInvocationContext Context(string method, string path, string? idempotencyKey)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        if (idempotencyKey is not null)
        {
            httpContext.Request.Headers[IdempotencyKeyEndpointFilter.HeaderName] = idempotencyKey;
        }

        return new TestEndpointFilterInvocationContext(httpContext);
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext) : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext => httpContext;

        public override IList<object?> Arguments => [];

        public override T GetArgument<T>(int index) => throw new InvalidOperationException("Tidak dipakai di test.");
    }

    // Store yang gagal saat Release — memverifikasi filter tidak menukar exception handler dengan exception store.
    private sealed class ReleaseThrowsStore : IApiIdempotencyStore
    {
        public Task<IdempotencyReservation> TryReserveAsync(
            string idempotencyKey,
            TimeSpan pendingTimeToLive,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IdempotencyReservation.Reserved(Guid.NewGuid()));

        public Task CompleteAsync(
            string idempotencyKey,
            Guid ownerToken,
            string response,
            TimeSpan timeToLive,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ReleaseAsync(string idempotencyKey, Guid ownerToken, CancellationToken cancellationToken = default) =>
            throw new TimeoutException("store tumbang saat release");
    }

    private sealed class RecordingIdempotencyStore : IApiIdempotencyStore
    {
        private readonly ConcurrentDictionary<string, IdempotencyEntry> _entries = new(StringComparer.Ordinal);

        // Berisi response yang sudah selesai disimpan.
        public IReadOnlyDictionary<string, string> Saved =>
            _entries
                .Where(entry => entry.Value.Response is not null)
                .ToDictionary(entry => entry.Key, entry => entry.Value.Response!, StringComparer.Ordinal);

        public Task<IdempotencyReservation> TryReserveAsync(
            string idempotencyKey,
            TimeSpan pendingTimeToLive,
            CancellationToken cancellationToken = default)
        {
            var ownerToken = Guid.NewGuid();
            if (_entries.TryAdd(idempotencyKey, IdempotencyEntry.Pending(ownerToken)))
            {
                return Task.FromResult(IdempotencyReservation.Reserved(ownerToken));
            }

            return Task.FromResult(
                _entries.TryGetValue(idempotencyKey, out var entry) && entry.Response is not null
                    ? IdempotencyReservation.Completed(entry.Response)
                    : IdempotencyReservation.Pending);
        }

        public Task CompleteAsync(
            string idempotencyKey,
            Guid ownerToken,
            string response,
            TimeSpan timeToLive,
            CancellationToken cancellationToken = default)
        {
            _entries.TryUpdate(idempotencyKey, IdempotencyEntry.Completed(response), IdempotencyEntry.Pending(ownerToken));
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(string idempotencyKey, Guid ownerToken, CancellationToken cancellationToken = default)
        {
            _entries.TryRemove(KeyValuePair.Create(idempotencyKey, IdempotencyEntry.Pending(ownerToken)));
            return Task.CompletedTask;
        }

        private sealed record IdempotencyEntry(string? Response, Guid OwnerToken)
        {
            public static IdempotencyEntry Pending(Guid ownerToken) => new(null, ownerToken);

            public static IdempotencyEntry Completed(string response) => new(response, Guid.Empty);
        }
    }
}
