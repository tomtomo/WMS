using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure;
using Wms.Auth.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Auth.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SoftDeleteFilterTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "P@ssw0rd-123";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = AuthTestHost.Build(connectionString);
        await AuthTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task A_disabled_user_is_hidden_from_the_default_reader()
    {
        var activeId = await AuthScenarios.CreateUserAsync(_provider, "active1", Password);
        var disabledId = await AuthScenarios.CreateUserAsync(_provider, "disabled1", Password);
        await AuthScenarios.DisableUserAsync(_provider, disabledId);

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IUserReader>();

        (await reader.GetByIdAsync(disabledId)).Should().BeNull("Disabled tersembunyi dari query default");
        (await reader.GetByIdAsync(activeId)).Should().NotBeNull();
    }

    [Fact]
    public async Task Include_inactive_reveals_the_disabled_user()
    {
        var activeId = await AuthScenarios.CreateUserAsync(_provider, "active2", Password);
        var disabledId = await AuthScenarios.CreateUserAsync(_provider, "disabled2", Password);
        await AuthScenarios.DisableUserAsync(_provider, disabledId);

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IUserReader>();

        var all = await reader.ListAsync(1, 50, includeInactive: true);
        all.Items.Select(user => user.UserId).Should().Contain([activeId, disabledId]);

        var activeOnly = await reader.ListAsync(1, 50, includeInactive: false);
        activeOnly.Items.Select(user => user.UserId).Should().Contain(activeId).And.NotContain(disabledId);
    }

    [Fact]
    public async Task Disable_is_a_soft_delete_the_row_still_persists()
    {
        var userId = await AuthScenarios.CreateUserAsync(_provider, "soft1", Password);
        await AuthScenarios.DisableUserAsync(_provider, userId);

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var rowStillExists = await context.Set<User>().IgnoreQueryFilters()
            .AnyAsync(user => user.Id == UserId.Create(userId).Value);

        rowStillExists.Should().BeTrue("Disable = soft delete, bukan hard delete");
    }
}
