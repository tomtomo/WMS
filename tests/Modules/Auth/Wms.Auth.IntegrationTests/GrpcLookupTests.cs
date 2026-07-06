using AwesomeAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Domain;
using Wms.Auth.Grpc.V1;
using Wms.Auth.Infrastructure;
using Wms.Auth.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// gRPC read API auth.v1
[Collection(PostgresCollection.Name)]
public sealed class GrpcLookupTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const int CatalogSize = 16;

    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await AuthTestHost.MigrateAndSeedAsync(_app.Services);

        _channel = GrpcChannel.ForAddress(
            _app.GetTestClient().BaseAddress!,
            new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() });
    }

    public async Task DisposeAsync()
    {
        _channel.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task GetPermissions_returns_the_full_catalog()
    {
        var client = new AuthLookup.AuthLookupClient(_channel);

        var catalog = await client.GetPermissionsAsync(new GetPermissionsRequest());

        catalog.Permissions.Should().HaveCount(CatalogSize);
        catalog.Permissions.Select(permission => permission.Code).Should().Contain("Auth.ManageUser");
    }

    [Fact]
    public async Task GetUser_returns_a_snapshot_with_effective_permissions()
    {
        var adminId = await FindIdAsync<User>(user => user.Username == "admin", user => user.Id.Value);
        var client = new AuthLookup.AuthLookupClient(_channel);

        var snapshot = await client.GetUserAsync(new GetUserRequest { UserId = adminId.ToString() });

        snapshot.UserId.Should().Be(adminId.ToString());
        snapshot.Username.Should().Be("admin");
        snapshot.IsActive.Should().BeTrue();
        snapshot.PermissionCodes.Should().HaveCount(CatalogSize);
    }

    [Fact]
    public async Task GetRole_returns_a_snapshot_with_permission_codes()
    {
        var adminRoleId = await FindIdAsync<Role>(role => role.Code == "Admin", role => role.Id.Value);
        var client = new AuthLookup.AuthLookupClient(_channel);

        var snapshot = await client.GetRoleAsync(new GetRoleRequest { RoleId = adminRoleId.ToString() });

        snapshot.Code.Should().Be("Admin");
        snapshot.PermissionCodes.Should().HaveCount(CatalogSize);
    }

    [Fact]
    public async Task GetUser_unknown_is_not_found_with_trailer_error_code()
    {
        var client = new AuthLookup.AuthLookupClient(_channel);

        var call = async () => await client.GetUserAsync(new GetUserRequest { UserId = Guid.NewGuid().ToString() });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
        exception.Which.Trailers.GetValue("error-code").Should().Be("user.not_found");
    }

    [Fact]
    public async Task GetUser_invalid_guid_is_invalid_argument()
    {
        var client = new AuthLookup.AuthLookupClient(_channel);

        var call = async () => await client.GetUserAsync(new GetUserRequest { UserId = "bukan-guid" });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    private async Task<Guid> FindIdAsync<TEntity>(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        Func<TEntity, Guid> selectId)
        where TEntity : class
    {
        using var scope = _app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var entity = await context.Set<TEntity>().IgnoreQueryFilters().FirstAsync(predicate);
        return selectId(entity);
    }
}
