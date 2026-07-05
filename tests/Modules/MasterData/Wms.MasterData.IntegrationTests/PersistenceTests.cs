using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.MasterData.Domain;
using Wms.MasterData.Infrastructure;
using Wms.MasterData.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.MasterData.IntegrationTests;

// Persistence EF (Testcontainers Postgres)
[Collection(PostgresCollection.Name)]
public sealed class PersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = MasterDataTestHost.Build(connectionString);
        await MasterDataTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Create_persists_the_row_with_audit_fields_filled_by_the_interceptor()
    {
        var id = await MasterDataScenarios.CreateWarehouseAsync(_provider, "DC Jakarta Cakung", "Jl. Raya Cakung No. 1");
        var warehouseId = WarehouseId.Create(id).Value;

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasterDataDbContext>();
        var warehouse = await context.Set<Warehouse>().FirstAsync(candidate => candidate.Id == warehouseId);

        warehouse.Name.Should().Be("DC Jakarta Cakung");
        warehouse.IsActive.Should().BeTrue();
        warehouse.CreatedBy.Should().Be(FixedCurrentUser.TestUserId, "audit di isi EF interceptor dari ICurrentUser");
    }

    [Fact]
    public async Task Concurrent_update_raises_a_concurrency_conflict_via_xmin()
    {
        var id = await MasterDataScenarios.CreateWarehouseAsync(_provider, "DC X", "Addr");
        var warehouseId = WarehouseId.Create(id).Value;

        using var scope1 = _provider.CreateScope();
        using var scope2 = _provider.CreateScope();
        var context1 = scope1.ServiceProvider.GetRequiredService<MasterDataDbContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<MasterDataDbContext>();

        var first = await context1.Set<Warehouse>().IgnoreQueryFilters().FirstAsync(candidate => candidate.Id == warehouseId);
        var second = await context2.Set<Warehouse>().IgnoreQueryFilters().FirstAsync(candidate => candidate.Id == warehouseId);

        first.Update("DC First", "Addr First");
        await context1.SaveChangesAsync();

        second.Update("DC Second", "Addr Second");
        var conflictingSave = async () => await context2.SaveChangesAsync();

        await conflictingSave.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
