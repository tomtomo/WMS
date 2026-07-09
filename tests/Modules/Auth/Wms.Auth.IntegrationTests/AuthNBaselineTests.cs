using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure;
using Wms.Auth.Infrastructure.Seed;
using Wms.Auth.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Xunit;

// Konfigurasi autentikasi untuk integration test.
namespace Wms.Auth.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AuthNBaselineTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await AuthTestHost.MigrateAndSeedAsync(_app.Services);
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task A_bearer_token_populates_current_user_so_audit_records_the_user_id()
    {
        var (token, adminUserId) = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await _client.PostAsJsonAsync(
            "/v1/roles",
            new { code = "AuditedRole", name = "Audited", permissionIds = Array.Empty<Guid>() });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var roleId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roleId").GetGuid();

        (await RoleCreatedByAsync(roleId)).Should().Be(
            adminUserId.ToString(),
            "audit CreatedBy = userId dari klaim 'sub' token (ICurrentUser terisi)");
    }

    [Fact]
    public async Task Without_a_token_the_audit_actor_falls_back_to_system()
    {
        var create = await _client.PostAsJsonAsync(
            "/v1/roles",
            new { code = "SystemRole", name = "System", permissionIds = Array.Empty<Guid>() });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var roleId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roleId").GetGuid();

        (await RoleCreatedByAsync(roleId)).Should().Be(ICurrentUser.SystemActor);
    }

    private async Task<(string Token, Guid UserId)> LoginAdminAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/v1/login",
            new { username = AuthSeeder.DefaultAdminUsername, password = AuthSeeder.DefaultAdminPassword });
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;

        using var scope = _app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var admin = await context.Set<User>().FirstAsync(user => user.Username == AuthSeeder.DefaultAdminUsername);
        return (token, admin.Id.Value);
    }

    private async Task<string> RoleCreatedByAsync(Guid roleId)
    {
        using var scope = _app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var role = await context.Set<Role>().IgnoreQueryFilters()
            .FirstAsync(candidate => candidate.Id == RoleId.Create(roleId).Value);
        return role.CreatedBy;
    }
}
