using AwesomeAssertions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Wms.Platform.Azure.ObjectStore;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// User delegation key dicache agar tidak perlu diminta lewat jaringan setiap kali.
// Key harus diupdate sebelum kedaluwarsa agar SAS tidak ditolak Storage dengan status 403.
public sealed class UserDelegationKeyCacheTests
{
    private static readonly TimeSpan _keyLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan _clockSkew = TimeSpan.FromMinutes(5);

    [Fact]
    public void Key_is_exchanged_once_and_then_reused()
    {
        var timeProvider = new FakeTimeProvider();
        var serviceClient = ClientReturning(timeProvider, "key-1");
        var cache = new UserDelegationKeyCache(serviceClient, timeProvider, _keyLifetime, _clockSkew);

        cache.Get().Value.Should().Be("key-1");
        cache.Get().Value.Should().Be("key-1");

        serviceClient.Received(1).GetUserDelegationKey(Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void Key_is_refreshed_before_it_expires_not_after()
    {
        var timeProvider = new FakeTimeProvider();
        var serviceClient = ClientReturning(timeProvider, "key-1", "key-2");
        var cache = new UserDelegationKeyCache(serviceClient, timeProvider, _keyLifetime, _clockSkew);
        cache.Get().Value.Should().Be("key-1");

        // Masuk ke 5 menit terakhir masa berlaku agar key diperbarui sebelum kedaluwarsa.
        timeProvider.Advance(_keyLifetime - TimeSpan.FromMinutes(4));

        cache.Get().Value.Should().Be("key-2");
    }

    [Fact]
    public void Concurrent_callers_exchange_the_key_only_once()
    {
        var timeProvider = new FakeTimeProvider();
        var serviceClient = ClientReturning(timeProvider, "key-1");
        var cache = new UserDelegationKeyCache(serviceClient, timeProvider, _keyLifetime, _clockSkew);

        var keys = new string[32];
        Parallel.For(0, keys.Length, index => keys[index] = cache.Get().Value);

        keys.Should().AllBe("key-1");
        serviceClient.Received(1).GetUserDelegationKey(Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset>());
    }

    private static BlobServiceClient ClientReturning(TimeProvider timeProvider, params string[] keyValues)
    {
        var serviceClient = Substitute.For<BlobServiceClient>();
        var responses = keyValues
            .Select(value => Response.FromValue(NewKey(timeProvider, value), Substitute.For<Response>()))
            .ToArray();

        var call = serviceClient.GetUserDelegationKey(Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset>());
        if (responses.Length == 1)
        {
            call.Returns(responses[0]);
        }
        else
        {
            call.Returns(responses[0], responses[1..]);
        }

        return serviceClient;
    }

    private static UserDelegationKey NewKey(TimeProvider timeProvider, string value) =>
        BlobsModelFactory.UserDelegationKey(
            "oid",
            "tid",
            timeProvider.GetUtcNow(),
            timeProvider.GetUtcNow().Add(_keyLifetime),
            "b",
            "2021-08-06",
            value);
}
