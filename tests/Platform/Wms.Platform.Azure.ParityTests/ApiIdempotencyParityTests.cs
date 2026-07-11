using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Wms.Platform.Local.Persistence;
using Wms.Platform.Shared.Cache;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan alur reserve, complete, release, dan replay berjalan sama di Postgres (Local) dan Redis (Azure/GCP).
// Owner token memastikan hanya pemilik reservation yang dapat menyelesaikan request.
public abstract class ApiIdempotencyParityTests
{
    private const string StoredResponse = """{"statusCode":201,"body":"{\"id\":42}"}""";

    private static readonly TimeSpan _pendingTimeToLive = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _completedTimeToLive = TimeSpan.FromHours(24);

    protected abstract IApiIdempotencyStore Store { get; }

    [Fact]
    public async Task First_caller_wins_the_claim()
    {
        var reservation = await Store.TryReserveAsync(NewKey(), _pendingTimeToLive);

        reservation.Status.Should().Be(IdempotencyReservationStatus.Reserved);
        reservation.OwnerToken.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Second_caller_sees_pending_while_the_first_still_runs()
    {
        var key = NewKey();
        await Store.TryReserveAsync(key, _pendingTimeToLive);

        var second = await Store.TryReserveAsync(key, _pendingTimeToLive);

        second.Status.Should().Be(IdempotencyReservationStatus.Pending);
    }

    [Fact]
    public async Task Completed_claim_replays_the_stored_response()
    {
        var key = NewKey();
        var reservation = await Store.TryReserveAsync(key, _pendingTimeToLive);

        await Store.CompleteAsync(key, reservation.OwnerToken, StoredResponse, _completedTimeToLive);

        var replay = await Store.TryReserveAsync(key, _pendingTimeToLive);
        replay.Status.Should().Be(IdempotencyReservationStatus.Completed);
        replay.StoredResponse.Should().Be(StoredResponse);
    }

    [Fact]
    public async Task Released_claim_can_be_reserved_again()
    {
        var key = NewKey();
        var reservation = await Store.TryReserveAsync(key, _pendingTimeToLive);

        await Store.ReleaseAsync(key, reservation.OwnerToken);

        (await Store.TryReserveAsync(key, _pendingTimeToLive)).Status.Should().Be(IdempotencyReservationStatus.Reserved);
    }

    [Fact]
    public async Task Non_owner_cannot_complete_someone_elses_claim()
    {
        var key = NewKey();
        await Store.TryReserveAsync(key, _pendingTimeToLive);

        await Store.CompleteAsync(key, Guid.NewGuid(), StoredResponse, _completedTimeToLive);

        (await Store.TryReserveAsync(key, _pendingTimeToLive)).Status.Should().Be(IdempotencyReservationStatus.Pending);
    }

    [Fact]
    public async Task Non_owner_cannot_release_someone_elses_claim()
    {
        var key = NewKey();
        await Store.TryReserveAsync(key, _pendingTimeToLive);

        await Store.ReleaseAsync(key, Guid.NewGuid());

        (await Store.TryReserveAsync(key, _pendingTimeToLive)).Status.Should().Be(IdempotencyReservationStatus.Pending);
    }

    [Fact]
    public async Task Expired_pending_claim_is_taken_over_by_the_next_caller()
    {
        var key = NewKey();
        await Store.TryReserveAsync(key, TimeSpan.FromSeconds(1));

        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        (await Store.TryReserveAsync(key, _pendingTimeToLive)).Status.Should().Be(IdempotencyReservationStatus.Reserved);
    }

    [Fact]
    public async Task Non_positive_pending_ttl_is_rejected()
    {
        var reserve = () => Store.TryReserveAsync(NewKey(), TimeSpan.Zero);

        await reserve.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private static string NewKey() => $"POST:/v1/goods-receipts:{Guid.NewGuid():N}";
}

[Collection(PostgresCollection.Name)]
public sealed class PostgresApiIdempotencyStoreParityTests : ApiIdempotencyParityTests
{
    public PostgresApiIdempotencyStoreParityTests(PostgresFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        Store = new PostgresApiIdempotencyStore(fixture.DataSource, TimeProvider.System);
    }

    protected override IApiIdempotencyStore Store { get; }
}

[Collection(RedisCollection.Name)]
public sealed class RedisApiIdempotencyStoreParityTests : ApiIdempotencyParityTests
{
    public RedisApiIdempotencyStoreParityTests(RedisFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        Store = new RedisApiIdempotencyStore(fixture.Multiplexer);
    }

    protected override IApiIdempotencyStore Store { get; }
}
