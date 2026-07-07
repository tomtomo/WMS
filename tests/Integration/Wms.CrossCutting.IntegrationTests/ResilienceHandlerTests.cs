using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.BuildingBlocks.Infrastructure.Resilience;
using Wms.MasterData.Grpc.V1;
using Xunit;

namespace Wms.CrossCutting.IntegrationTests;

// Memastikan klien s2s memakai urutan resilience handler yang sesuai.
public sealed class ResilienceHandlerTests
{
    [Fact]
    public void Split_timeout_profil_http_5s_dan_grpc_30s()
    {
        var http = new HttpStandardResilienceOptions();
        ResiliencePipelineDefaults.ConfigureHttp(http);
        http.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        http.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(2));

        var grpc = new HttpStandardResilienceOptions();
        ResiliencePipelineDefaults.ConfigureGrpc(grpc);
        grpc.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        grpc.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task AddInternalGrpcClient_memasang_standard_handler_profil_grpc()
    {
        var services = new ServiceCollection();
        services.AddInternalGrpcClient<MasterDataLookup.MasterDataLookupClient>(new Uri("http://localhost:1"));
        var provider = services.BuildServiceProvider();
        await using (provider.ConfigureAwait(false))
        {
            provider.GetRequiredService<MasterDataLookup.MasterDataLookupClient>().Should().NotBeNull();

            var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>()
                .CreateHandler(nameof(MasterDataLookup.MasterDataLookupClient));
            HandlerChain(handler).Should().Contain(
                name => name.Contains("Resilience", StringComparison.Ordinal),
                "klien gRPC internal wajib ber-AddStandardResilienceHandler");

            var options = provider.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
                .Get($"{nameof(MasterDataLookup.MasterDataLookupClient)}-standard");
            options.TotalRequestTimeout.Timeout.Should().Be(
                ResiliencePipelineDefaults.GrpcTotalTimeout,
                "profil gRPC (30s) harus terikat ke klien, bukan profil HTTP");
        }
    }

    [Fact]
    public async Task Profil_grpc_menggantikan_default_http_global_bukan_bertumpuk()
    {
        var services = new ServiceCollection();

        // Urutan host: default global dulu (AddDefaultHttpClientDefaults), lalu klien gRPC internal.
        services.ConfigureHttpClientDefaults(http => http.AddHttpResilience());
        services.AddInternalGrpcClient<MasterDataLookup.MasterDataLookupClient>(new Uri("http://localhost:1"));

        var provider = services.BuildServiceProvider();
        await using (provider.ConfigureAwait(false))
        {
            var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>()
                .CreateHandler(nameof(MasterDataLookup.MasterDataLookupClient));

            HandlerChain(handler).Count(name => name.Contains("Resilience", StringComparison.Ordinal)).Should().Be(
                1,
                "profil gRPC harus menggantikan default HTTP global — jika bertumpuk, total timeout 5s memangkas budget 30s gRPC");
        }
    }

    [Fact]
    public async Task Default_http_global_ber_resilience_handler()
    {
        var services = new ServiceCollection();
        services.ConfigureHttpClientDefaults(http => http.AddHttpResilience());
        var provider = services.BuildServiceProvider();
        await using (provider.ConfigureAwait(false))
        {
            var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("any-client");
            HandlerChain(handler).Should().Contain(name => name.Contains("Resilience", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Error_transient_di_retry_lalu_pulih()
    {
        var stub = new ScriptedHandler(HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError, HttpStatusCode.OK);
        var provider = BuildClient("wms-retry", stub, options =>
        {
            ResiliencePipelineDefaults.ConfigureHttp(options);
            options.Retry.Delay = TimeSpan.Zero;
        });
        await using (provider.ConfigureAwait(false))
        {
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("wms-retry");

            var response = await client.GetAsync(new Uri("http://stub.local/ping"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            stub.Calls.Should().Be(3, "dua kegagalan transient di-retry lalu pulih");
        }
    }

    [Fact]
    public async Task Dependency_lambat_dipotong_timeout()
    {
        var stub = new HangingHandler();
        var provider = BuildClient("wms-timeout", stub, options =>
        {
            ResiliencePipelineDefaults.ConfigureHttp(options);

            // Gunakan timeout pendek agar test selesai lebih cepat.
            options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(50);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMilliseconds(400);
            options.Retry.Delay = TimeSpan.Zero;
            options.Retry.MaxRetryAttempts = 1;
        });
        await using (provider.ConfigureAwait(false))
        {
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("wms-timeout");
            var stopwatch = Stopwatch.StartNew();

            var act = async () => await client.GetAsync(new Uri("http://stub.local/slow"));

            var thrown = (await act.Should().ThrowAsync<Exception>()).Which;
            (thrown is TimeoutRejectedException or TaskCanceledException).Should().BeTrue(
                $"dependency lambat harus dipotong timeout, bukan menggantung (dapat {thrown.GetType().Name})");
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5), "timeout memotong jauh sebelum menggantung");
        }
    }

    [Fact]
    public async Task Error_beruntun_membuka_circuit_breaker_fail_fast()
    {
        var stub = new ScriptedHandler();
        var provider = BuildClient("wms-breaker", stub, options =>
        {
            ResiliencePipelineDefaults.ConfigureHttp(options);
            options.Retry.Delay = TimeSpan.Zero;
            options.Retry.MaxRetryAttempts = 1;
            options.CircuitBreaker.MinimumThroughput = 2;
            options.CircuitBreaker.FailureRatio = 0.1;

            // Tahan circuit tetap open agar assertion fail fast stabil di CI.
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
        });
        await using (provider.ConfigureAwait(false))
        {
            var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("wms-breaker");

            var broke = false;
            for (var attempt = 0; attempt < 10 && !broke; attempt++)
            {
                try
                {
                    await client.GetAsync(new Uri("http://stub.local/down"));
                }
                catch (BrokenCircuitException)
                {
                    broke = true;
                }
            }

            broke.Should().BeTrue("error beruntun harus membuka circuit breaker");

            // Fail fast: Saat circuit open, request langsung gagal tanpa memanggil dependency.
            var callsWhenOpen = stub.Calls;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                    await client.GetAsync(new Uri("http://stub.local/down")));
            }

            stub.Calls.Should().Be(callsWhenOpen, "circuit open = fail-fast tanpa memanggil dependency");
        }
    }

    [Fact]
    public void Dlq_retry_tetap_manual_konsumen_3x_dan_producer_5x()
    {
        // replay DLQ manual loop
        ConsumerDeadLetterPipeline.MaxAttempts.Should().Be(3);
        OutboxDispatcher.MaxPublishAttempts.Should().Be(5);
    }

    private static ServiceProvider BuildClient(
        string name,
        HttpMessageHandler primaryHandler,
        Action<HttpStandardResilienceOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(name)
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler)
            .AddStandardResilienceHandler()
            .Configure(configure);
        return services.BuildServiceProvider();
    }

    private static List<string> HandlerChain(HttpMessageHandler handler)
    {
        var chain = new List<string>();
        for (var current = handler; current is not null; current = (current as DelegatingHandler)?.InnerHandler)
        {
            chain.Add(current.GetType().Name);
        }

        return chain;
    }

    // skenario respons berurutan. kosong = selalu 500.
    private sealed class ScriptedHandler(params HttpStatusCode[] script) : HttpMessageHandler
    {
        private int _calls;

        public int Calls => _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref _calls);
            var status = call <= script.Length ? script[call - 1] : HttpStatusCode.InternalServerError;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
