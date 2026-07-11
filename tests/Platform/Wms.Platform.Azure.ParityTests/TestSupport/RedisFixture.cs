using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Wms.Platform.Azure.ParityTests.TestSupport;

// Managed Redis memakai protokol RESP, jadi container redis biasa sudah cukup menjadi proxy untuk adapter yang sama.
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public IConnectionMultiplexer Multiplexer { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        Multiplexer = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (Multiplexer is not null)
        {
            await Multiplexer.DisposeAsync();
        }

        await _redis.DisposeAsync();
    }

    public Task FlushAsync() => Multiplexer.GetServers()[0].FlushDatabaseAsync();
}

[CollectionDefinition(Name)]
public sealed class RedisCollection : ICollectionFixture<RedisFixture>
{
    public const string Name = "redis";
}
