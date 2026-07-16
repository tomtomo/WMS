using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure;
using Wms.Auth.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Integration test untuk seeding permission.
[Collection(PostgresCollection.Name)]
public sealed class PermissionSeedTests(PostgresFixture postgres) : IAsyncLifetime
{
    // Katalog berisi 23 permission lama dan Outbound.CreateOrder.
    private const int CatalogSize = 24;

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = AuthTestHost.Build(connectionString);
        await AuthTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Seed_populates_the_full_permission_catalog_idempotently()
    {
        await AuthTestHost.SeedAsync(_provider);
        await AuthTestHost.SeedAsync(_provider);

        using var scope = _provider.CreateScope();
        var permissions = await scope.ServiceProvider.GetRequiredService<IPermissionReader>().ListAsync();

        permissions.Select(permission => permission.Code)
            .Should().Contain(["Inbound.PostGR", "Auth.ManageUser", "MasterData.ManageWarehouse", "Outbound.DispatchWave"]);
        permissions.Select(permission => permission.Code).Should().OnlyHaveUniqueItems();
        permissions.Should().HaveCount(CatalogSize, "seed ulang tidak menduplikat");
    }

    [Fact]
    public async Task Seed_creates_default_roles_and_the_admin_user()
    {
        await AuthTestHost.SeedAsync(_provider);

        using var scope = _provider.CreateScope();
        var roles = await scope.ServiceProvider.GetRequiredService<IRoleReader>().ListAsync(1, 50);
        roles.Items.Select(role => role.Code)
            .Should().Contain(["Admin", "WarehouseManager", "Supervisor", "Operator", "Viewer"]);

        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        (await context.Set<User>().AnyAsync(user => user.Username == "admin")).Should().BeTrue();
    }

    [Fact]
    public async Task The_admin_users_effective_permissions_are_the_distinct_catalog_union()
    {
        await AuthTestHost.SeedAsync(_provider);

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var admin = await context.Set<User>().FirstAsync(user => user.Username == "admin");

        var dto = await scope.ServiceProvider.GetRequiredService<IUserReader>().GetByIdAsync(admin.Id.Value);

        dto.Should().NotBeNull();
        dto!.PermissionCodes.Should().HaveCount(CatalogSize).And.OnlyHaveUniqueItems();
        dto.PermissionCodes.Should().Contain("Auth.ManageUser");
    }

    [Fact]
    public async Task Two_overlapping_roles_resolve_to_the_distinct_union_without_duplicates()
    {
        await AuthTestHost.SeedAsync(_provider);

        Guid postGr;
        Guid scanGr;
        Guid holdGr;
        using (var seedScope = _provider.CreateScope())
        {
            var catalog = await seedScope.ServiceProvider.GetRequiredService<IPermissionReader>().ListAsync();
            postGr = catalog.First(permission => permission.Code == "Inbound.PostGR").PermissionId;
            scanGr = catalog.First(permission => permission.Code == "Inbound.ScanGR").PermissionId;
            holdGr = catalog.First(permission => permission.Code == "Inbound.HoldGR").PermissionId;
        }

        // Dua role dengan permission tumpang tindih di Inbound.ScanGR.
        var roleA = await AuthScenarios.CreateRoleAsync(_provider, "OverlapA", "Overlap A", [postGr, scanGr]);
        var roleB = await AuthScenarios.CreateRoleAsync(_provider, "OverlapB", "Overlap B", [scanGr, holdGr]);
        var userId = await AuthScenarios.CreateUserAsync(_provider, "overlapuser", "P@ssw0rd-123", roleIds: [roleA, roleB]);

        using var scope = _provider.CreateScope();
        var dto = await scope.ServiceProvider.GetRequiredService<IUserReader>().GetByIdAsync(userId);

        dto.Should().NotBeNull();
        dto!.PermissionCodes.Should().HaveCount(3).And.OnlyHaveUniqueItems("union tanpa duplikat meski dua role overlap");
        dto.PermissionCodes.Should().Contain("Inbound.PostGR").And.Contain("Inbound.ScanGR").And.Contain("Inbound.HoldGR");
    }

    // Seed ulang harus melengkapi role Admin lama, termasuk saat database berasal dari persistent volume.
    [Fact]
    public async Task Seed_reconciles_existing_admin_role_to_full_catalog()
    {
        await AuthTestHost.SeedAsync(_provider);

        // Buat kondisi role Admin lama dengan menghapus satu permission.
        using (var scope = _provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var admin = await context.Set<Role>().IgnoreQueryFilters().FirstAsync(role => role.Code == "Admin");
            var catalog = await context.Set<Permission>().ToListAsync();
            var trimmed = catalog.Skip(1).Select(permission => permission.Id.Value);
            admin.SetPermissions(trimmed);
            await context.SaveChangesAsync();
        }

        await AuthTestHost.SeedAsync(_provider);

        using (var scope = _provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var admin = await context.Set<Role>().IgnoreQueryFilters().FirstAsync(role => role.Code == "Admin");
            admin.PermissionIds.Should().HaveCount(CatalogSize, "admin di-reconcile ke full catalog meski role sudah ada");
        }
    }

    // Seed pengguna Viewer untuk test visibilitas tombol dan respons 403 pada kriteria 6.
    [Fact]
    public async Task Seed_creates_a_read_only_viewer_user_without_write_permissions()
    {
        await AuthTestHost.SeedAsync(_provider);

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var viewer = await context.Set<User>().FirstOrDefaultAsync(user => user.Username == "viewer");
        viewer.Should().NotBeNull("low-perm user untuk probe ⑥ harus ter-seed");

        var dto = await scope.ServiceProvider.GetRequiredService<IUserReader>().GetByIdAsync(viewer!.Id.Value);
        dto.Should().NotBeNull();
        dto!.PermissionCodes.Should().Contain("Outbound.Read", "Viewer boleh baca");
        dto.PermissionCodes.Should().NotContain("Outbound.CreateOrder", "Viewer tak boleh aksi gated");
        dto.PermissionCodes.Should().NotContain("Outbound.CreateWave");
        dto.PermissionCodes.Should().NotContain("Inbound.CreateGR");
    }
}
