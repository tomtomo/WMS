using AwesomeAssertions;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Wms.Auth.Grpc.Client;
using Wms.Auth.Grpc.V1;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Pastikan status aktif user dicache, user yang tidak ditemukan dianggap nonaktif, dan request tetap lanjut saat Auth tidak tersedia.
public sealed class AuthGrpcActiveUserCheckerTests
{
    private static readonly string _userId = Guid.NewGuid().ToString();

    [Fact]
    public async Task Caches_the_answer_within_ttl_and_refreshes_after_expiry()
    {
        var time = new FakeTimeProvider();
        var client = new FakeAuthLookupClient { IsActive = true };
        var checker = NewChecker(client, time);

        (await checker.IsActiveAsync(_userId)).Should().BeTrue();
        (await checker.IsActiveAsync(_userId)).Should().BeTrue();
        client.CallCount.Should().Be(1, "jawaban kedua datang dari cache TTL");

        time.Advance(TimeSpan.FromSeconds(61));
        client.IsActive = false;
        (await checker.IsActiveAsync(_userId)).Should().BeFalse("TTL lewat, jawaban direfresh dari Auth");
        client.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Unknown_user_counts_as_inactive()
    {
        var client = new FakeAuthLookupClient { Failure = new RpcException(new Status(StatusCode.NotFound, "no user")) };
        var checker = NewChecker(client, new FakeTimeProvider());

        (await checker.IsActiveAsync(_userId)).Should().BeFalse();
    }

    [Fact]
    public async Task Unreachable_auth_fails_open()
    {
        var client = new FakeAuthLookupClient { Failure = new RpcException(new Status(StatusCode.Unavailable, "down")) };
        var checker = NewChecker(client, new FakeTimeProvider());

        (await checker.IsActiveAsync(_userId)).Should().BeTrue("JWT tetap gerbang utama saat Auth tak terjangkau");
    }

    private static AuthGrpcActiveUserChecker NewChecker(FakeAuthLookupClient client, TimeProvider time) =>
        new(new FakeGrpcClientFactory(client), time, NullLogger<AuthGrpcActiveUserChecker>.Instance);

    private sealed class FakeGrpcClientFactory(AuthLookup.AuthLookupClient client) : GrpcClientFactory
    {
        public override TClient CreateClient<TClient>(string name)
            where TClient : class => (TClient)(object)client;
    }

    private sealed class FakeAuthLookupClient : AuthLookup.AuthLookupClient
    {
        public bool IsActive { get; set; }

        public RpcException? Failure { get; set; }

        public int CallCount { get; private set; }

        public override AsyncUnaryCall<UserSnapshot> GetUserAsync(
            GetUserRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
        {
            CallCount++;
            var response = Failure is null
                ? Task.FromResult(new UserSnapshot { UserId = request.UserId, IsActive = IsActive })
                : Task.FromException<UserSnapshot>(Failure);
            return new AsyncUnaryCall<UserSnapshot>(
                response,
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => [],
                () => { });
        }
    }
}
