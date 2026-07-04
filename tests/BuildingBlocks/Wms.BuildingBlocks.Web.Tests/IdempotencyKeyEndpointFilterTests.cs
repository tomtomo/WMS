using System.Collections.Concurrent;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
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

    private sealed class RecordingIdempotencyStore : IApiIdempotencyStore
    {
        public ConcurrentDictionary<string, string> Saved { get; } = new(StringComparer.Ordinal);

        public Task<string?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Saved.TryGetValue(idempotencyKey, out var response) ? response : null);

        public Task SaveResponseAsync(
            string idempotencyKey,
            string response,
            TimeSpan timeToLive,
            CancellationToken cancellationToken = default)
        {
            Saved[idempotencyKey] = response;
            return Task.CompletedTask;
        }
    }
}
