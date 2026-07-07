using AwesomeAssertions;
using Npgsql;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.Persistence;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PostgresApiIdempotencyStoreTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset _epoch = new(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan _pendingTtl = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan _responseTtl = TimeSpan.FromHours(1);

    [Fact]
    public async Task First_reservation_wins()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(_epoch));

            var reservation = await store.TryReserveAsync("idem-1", _pendingTtl);

            reservation.Status.Should().Be(IdempotencyReservationStatus.Reserved);
        }
    }

    [Fact]
    public async Task Completed_reservation_replays_within_ttl()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(_epoch));
            var reservation = await store.TryReserveAsync("idem-2", _pendingTtl);
            await store.CompleteAsync("idem-2", reservation.OwnerToken, """{"grId":7}""", _responseTtl);

            var replay = await store.TryReserveAsync("idem-2", _pendingTtl);

            replay.Status.Should().Be(IdempotencyReservationStatus.Completed);
            replay.StoredResponse.Should().Be("""{"grId":7}""");
        }
    }

    [Fact]
    public async Task Expired_completed_entry_can_be_reserved_again()
    {
        var clock = new MutableTimeProvider(_epoch);
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, clock);
            var reservation = await store.TryReserveAsync("idem-3", _pendingTtl);
            await store.CompleteAsync("idem-3", reservation.OwnerToken, "resp", TimeSpan.FromMinutes(30));

            clock.Advance(TimeSpan.FromMinutes(31));

            (await store.TryReserveAsync("idem-3", _pendingTtl)).Status
                .Should().Be(IdempotencyReservationStatus.Reserved, "kuitansi kedaluwarsa boleh diambil alih");
        }
    }

    [Fact]
    public async Task Late_complete_does_not_overwrite_completed_receipt()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(_epoch));
            var reservation = await store.TryReserveAsync("idem-4", _pendingTtl);
            await store.CompleteAsync("idem-4", reservation.OwnerToken, "respons-pertama", _responseTtl);

            // Pemilik lama mencoba menulis ulang response yang sudah completed.
            await store.CompleteAsync("idem-4", reservation.OwnerToken, "respons-retry", _responseTtl);

            var replay = await store.TryReserveAsync("idem-4", _pendingTtl);
            replay.StoredResponse.Should().Be("respons-pertama", "kuitansi hidup first-wins");
        }
    }

    [Fact]
    public async Task Pending_claim_blocks_other_reservers()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(_epoch));
            await store.TryReserveAsync("idem-5", _pendingTtl);

            var second = await store.TryReserveAsync("idem-5", _pendingTtl);

            second.Status.Should().Be(IdempotencyReservationStatus.Pending);
            second.StoredResponse.Should().BeNull();
        }
    }

    [Fact]
    public async Task Expired_pending_claim_is_taken_over()
    {
        var clock = new MutableTimeProvider(_epoch);
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, clock);
            await store.TryReserveAsync("idem-6", _pendingTtl);

            clock.Advance(_pendingTtl + TimeSpan.FromSeconds(1));

            (await store.TryReserveAsync("idem-6", _pendingTtl)).Status
                .Should().Be(IdempotencyReservationStatus.Reserved, "klaim pending mati tidak boleh mengunci key selamanya");
        }
    }

    [Fact]
    public async Task Release_frees_the_key_for_the_next_reserver()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(_epoch));
            var reservation = await store.TryReserveAsync("idem-7", _pendingTtl);

            await store.ReleaseAsync("idem-7", reservation.OwnerToken);

            (await store.TryReserveAsync("idem-7", _pendingTtl)).Status
                .Should().Be(IdempotencyReservationStatus.Reserved);
        }
    }

    [Fact]
    public async Task Release_does_not_remove_completed_receipt()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(_epoch));
            var reservation = await store.TryReserveAsync("idem-8", _pendingTtl);
            await store.CompleteAsync("idem-8", reservation.OwnerToken, "resp", _responseTtl);

            await store.ReleaseAsync("idem-8", reservation.OwnerToken);

            var replay = await store.TryReserveAsync("idem-8", _pendingTtl);
            replay.Status.Should().Be(IdempotencyReservationStatus.Completed);
        }
    }

    [Fact]
    public async Task Concurrent_reservations_have_exactly_one_winner()
    {
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(_epoch));

            var reservations = await Task.WhenAll(
                Enumerable.Range(0, 12).Select(_ => store.TryReserveAsync("idem-race", _pendingTtl)));

            reservations.Count(reservation => reservation.Status == IdempotencyReservationStatus.Reserved)
                .Should().Be(1, "INSERT ON CONFLICT menjamin satu pemenang klaim");
            reservations.Count(reservation => reservation.Status == IdempotencyReservationStatus.Pending)
                .Should().Be(11);
        }
    }

    [Fact]
    public async Task Late_owner_complete_cannot_hijack_new_owner_claim()
    {
        var clock = new MutableTimeProvider(_epoch);
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, clock);
            var lateOwner = await store.TryReserveAsync("idem-fence", _pendingTtl);

            clock.Advance(_pendingTtl + TimeSpan.FromSeconds(1));
            var newOwner = await store.TryReserveAsync("idem-fence", _pendingTtl);
            newOwner.Status.Should().Be(IdempotencyReservationStatus.Reserved);

            // Pemilik telat menyelesaikan eksekusinya — fencing token menolak menimpa klaim pemilik baru.
            await store.CompleteAsync("idem-fence", lateOwner.OwnerToken, "respons-telat", _responseTtl);

            (await store.TryReserveAsync("idem-fence", _pendingTtl)).Status
                .Should().Be(IdempotencyReservationStatus.Pending, "klaim pemilik baru masih hidup, bukan kuitansi pemilik telat");

            await store.CompleteAsync("idem-fence", newOwner.OwnerToken, "respons-pemilik-baru", _responseTtl);
            (await store.TryReserveAsync("idem-fence", _pendingTtl)).StoredResponse
                .Should().Be("respons-pemilik-baru");
        }
    }

    [Fact]
    public async Task Late_owner_release_cannot_free_new_owner_claim()
    {
        var clock = new MutableTimeProvider(_epoch);
        var dataSource = NpgsqlDataSource.Create(await fixture.CreateFreshDatabaseAsync());
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, clock);
            var lateOwner = await store.TryReserveAsync("idem-fence-2", _pendingTtl);

            clock.Advance(_pendingTtl + TimeSpan.FromSeconds(1));
            (await store.TryReserveAsync("idem-fence-2", _pendingTtl)).Status
                .Should().Be(IdempotencyReservationStatus.Reserved);

            // Pemilik telat gagal lalu melepas klaim — token lama tidak boleh membebaskan klaim pemilik baru.
            await store.ReleaseAsync("idem-fence-2", lateOwner.OwnerToken);

            (await store.TryReserveAsync("idem-fence-2", _pendingTtl)).Status
                .Should().Be(IdempotencyReservationStatus.Pending, "reserver ketiga tetap tertolak — klaim pemilik baru utuh");
        }
    }

    [Fact]
    public async Task Legacy_rows_without_status_replay_as_completed()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync();

        // Simulasikan tabel lama dengan satu response tersimpan.
        var seed = new NpgsqlConnection(connectionString);
        await using (seed.ConfigureAwait(false))
        {
            await seed.OpenAsync();
            var command = seed.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = """
                    CREATE SCHEMA IF NOT EXISTS infrastructure;
                    CREATE TABLE infrastructure.api_idempotency (
                        idempotency_key text PRIMARY KEY,
                        response text NOT NULL,
                        expires_at timestamptz NOT NULL);
                    INSERT INTO infrastructure.api_idempotency VALUES ('idem-legacy', 'respons-lawas', '2026-07-03T10:00:00Z');
                    """;
                await command.ExecuteNonQueryAsync();
            }
        }

        var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (dataSource.ConfigureAwait(false))
        {
            var store = new PostgresApiIdempotencyStore(dataSource, new MutableTimeProvider(_epoch));

            var replay = await store.TryReserveAsync("idem-legacy", _pendingTtl);

            replay.Status.Should().Be(IdempotencyReservationStatus.Completed, "upgrade skema in-place: baris lama = completed");
            replay.StoredResponse.Should().Be("respons-lawas");
        }
    }
}
