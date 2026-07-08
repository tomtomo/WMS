using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure;
using Wms.Auth.Infrastructure.Security;
using Wms.Auth.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Memastikan authorization bekerja dari JWT sampai eksekusi endpoint.
[Collection(PostgresCollection.Name)]
public sealed class AuthorizationEnforcementTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly JwtIssuerOptions _jwtOptions = new()
    {
        Issuer = TestJwtKeys.Issuer,
        Audience = TestJwtKeys.Audience,
        SigningKeySecretName = TestJwtKeys.SigningKeySecretName,
        AccessTokenLifetimeMinutes = 15,
    };

    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private Guid _adminId;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString, enableAuthorization: true);
        await AuthTestHost.MigrateAndSeedAsync(_app.Services);
        _client = _app.GetTestClient();
        _adminId = await FindActiveUserIdAsync("admin");
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Anonymous_request_to_a_protected_endpoint_is_401()
    {
        var response = await _client.PostAsJsonAsync("/v1/auth/users", CreateUserBody());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_without_the_required_permission_is_403()
    {
        Authorize(await IssueTokenAsync(_adminId, "Inbound.ReadGR"));

        var response = await _client.PostAsJsonAsync("/v1/auth/users", CreateUserBody());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authenticated_with_the_permission_reaches_the_handler()
    {
        Authorize(await IssueTokenAsync(_adminId, "Auth.ManageUser"));

        var response = await _client.PostAsJsonAsync("/v1/auth/users", CreateUserBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_disabled_user_token_is_rejected_even_with_the_permission()
    {
        var disabledId = await CreateDisabledUserAsync();
        Authorize(await IssueTokenAsync(disabledId, "Auth.ManageUser"));

        var response = await _client.PostAsJsonAsync("/v1/auth/users", CreateUserBody());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static object CreateUserBody() => new
    {
        username = "u" + Guid.NewGuid().ToString("N")[..10],
        email = "new-user@wms.local",
        password = "P@ssw0rd-123",
        roleIds = Array.Empty<Guid>(),
        assignedWarehouseIds = Array.Empty<Guid>(),
    };

    private static async Task<string> IssueTokenAsync(Guid subjectId, params string[] permissions)
    {
        var issuer = new JwtTokenIssuer(new TestSecretProvider(), Options.Create(_jwtOptions), TimeProvider.System);
        var subject = User.Create(UserId.Create(subjectId).Value, "token-subject", "sub@wms.local", "hash", [], []).Value;
        var token = await issuer.IssueAsync(subject, permissions, CancellationToken.None);
        return token.Token;
    }

    private void Authorize(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<Guid> FindActiveUserIdAsync(string username)
    {
        using var scope = _app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await context.Set<User>().IgnoreQueryFilters().FirstAsync(candidate => candidate.Username == username);
        return user.Id.Value;
    }

    private async Task<Guid> CreateDisabledUserAsync()
    {
        using var scope = _app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = User.Create(
            UserId.Create(Guid.NewGuid()).Value,
            "disabled_" + Guid.NewGuid().ToString("N")[..8],
            "disabled@wms.local",
            hasher.Hash("P@ssw0rd-123"),
            [],
            []).Value;
        user.Disable();
        context.Add(user);
        await context.SaveChangesAsync();
        return user.Id.Value;
    }
}
