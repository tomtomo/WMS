using AwesomeAssertions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web;
using Wms.BuildingBlocks.Web.GrpcInterceptors;
using Wms.BuildingBlocks.Web.Tests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class GrpcInterceptorTests
{
    [Theory]
    [InlineData(ResultErrorType.Validation, StatusCode.InvalidArgument)]
    [InlineData(ResultErrorType.NotFound, StatusCode.NotFound)]
    [InlineData(ResultErrorType.Conflict, StatusCode.Aborted)]
    [InlineData(ResultErrorType.Failure, StatusCode.FailedPrecondition)]
    public void GrpcStatusMapper_aligns_error_class_with_grpc_status(ResultErrorType errorType, StatusCode expected)
    {
        GrpcStatusMapper.ToStatusCode(errorType).Should().Be(expected);
    }

    [Fact]
    public async Task ErrorMapping_maps_result_failure_to_grpc_status_and_error_code_trailer()
    {
        var interceptor = new ErrorMappingInterceptor(NullLogger<ErrorMappingInterceptor>.Instance);
        var context = new FakeServerCallContext(new Metadata());
        var error = new Error("inventory.not_found", "Stok tak ada.");
        UnaryServerMethod<string, string> continuation =
            (_, _) => throw new ResultFailureException(ResultErrorType.NotFound, error);

        var act = () => interceptor.UnaryServerHandler("req", context, continuation);

        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.NotFound);
        thrown.Which.Trailers.GetValue(GrpcStatusMapper.ErrorCodeTrailer).Should().Be("inventory.not_found");
    }

    [Fact]
    public async Task ErrorMapping_maps_unexpected_exception_to_internal_without_leaking_detail()
    {
        var interceptor = new ErrorMappingInterceptor(NullLogger<ErrorMappingInterceptor>.Instance);
        var context = new FakeServerCallContext(new Metadata());
        UnaryServerMethod<string, string> continuation =
            (_, _) => throw new InvalidOperationException("secret internal detail");

        var act = () => interceptor.UnaryServerHandler("req", context, continuation);

        var thrown = await act.Should().ThrowAsync<RpcException>();
        thrown.Which.StatusCode.Should().Be(StatusCode.Internal);
        thrown.Which.Status.Detail.Should().NotContain("secret internal detail");
    }

    [Fact]
    public async Task CorrelationId_propagates_inbound_metadata_to_scope_and_trailer()
    {
        var logger = new ScopeCapturingLogger<CorrelationIdInterceptor>();
        var requestHeaders = new Metadata { { CorrelationIdInterceptor.MetadataKey, "corr-9" } };
        var context = new FakeServerCallContext(requestHeaders);
        var interceptor = new CorrelationIdInterceptor(logger);
        UnaryServerMethod<string, string> continuation = (_, _) => Task.FromResult("ok");

        var response = await interceptor.UnaryServerHandler("req", context, continuation);

        response.Should().Be("ok");
        context.ResponseTrailers.GetValue(CorrelationIdInterceptor.MetadataKey).Should().Be("corr-9");
        var scope = logger.Scopes.Should().ContainSingle()
            .Which.Should().BeAssignableTo<IReadOnlyDictionary<string, object>>().Subject;
        scope[CorrelationId.LogScopeKey].Should().Be("corr-9");
    }

    [Fact]
    public async Task CorrelationId_generates_a_guid_when_metadata_absent()
    {
        var logger = new ScopeCapturingLogger<CorrelationIdInterceptor>();
        var context = new FakeServerCallContext(new Metadata());
        var interceptor = new CorrelationIdInterceptor(logger);
        UnaryServerMethod<string, string> continuation = (_, _) => Task.FromResult("ok");

        await interceptor.UnaryServerHandler("req", context, continuation);

        var echoed = context.ResponseTrailers.GetValue(CorrelationIdInterceptor.MetadataKey);
        Guid.TryParse(echoed, out _).Should().BeTrue();
    }

    private sealed class FakeServerCallContext(Metadata requestHeaders) : ServerCallContext
    {
        private readonly Metadata _responseTrailers = new();

        protected override string MethodCore => "/wms.test.Service/Method";

        protected override string HostCore => "localhost";

        protected override string PeerCore => "ipv4:127.0.0.1";

        protected override DateTime DeadlineCore => DateTime.MaxValue;

        protected override Metadata RequestHeadersCore { get; } = requestHeaders;

        protected override CancellationToken CancellationTokenCore => CancellationToken.None;

        protected override Metadata ResponseTrailersCore => _responseTrailers;

        protected override Status StatusCore { get; set; }

        protected override WriteOptions? WriteOptionsCore { get; set; }

        protected override AuthContext AuthContextCore => throw new NotSupportedException();

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
            => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
