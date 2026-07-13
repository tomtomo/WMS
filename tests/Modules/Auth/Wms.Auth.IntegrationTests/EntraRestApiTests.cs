using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure;
using Wms.Auth.Infrastructure.Seed;
using Wms.Auth.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Test endpoint login Entra dengan token dan JWKS khusus test hingga menghasilkan token internal.
[Collection(PostgresCollection.Name)]
public sealed class EntraRestApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(
            connectionString, entraConfigurationManager: TestEntraTokens.ConfigurationManager());
        await AuthTestHost.MigrateAndSeedAsync(_app.Services);
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task A_linked_entra_identity_gets_200_with_camel_case_tokens()
    {
        await LinkSeededAdminAsync("oid-rest");

        var response = await _client.PostAsJsonAsync(
            "/v1/login/entra", new { idToken = TestEntraTokens.Mint("oid-rest") });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"accessToken\"").And.Contain("\"refreshToken\"");
    }

    [Fact]
    public async Task An_unlinked_identity_gets_400_problem_details()
    {
        var response = await _client.PostAsJsonAsync(
            "/v1/login/entra", new { idToken = TestEntraTokens.Mint("oid-nobody") });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private async Task LinkSeededAdminAsync(string subject)
    {
        using var scope = _app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var externalLogins = scope.ServiceProvider.GetRequiredService<IUserExternalLoginRepository>();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var admin = await users.GetByUsernameAsync(AuthSeeder.DefaultAdminUsername);
        var link = UserExternalLogin.Link(
            UserExternalLoginId.Create(Guid.NewGuid()).Value,
            ExternalLoginProviders.Entra,
            subject,
            admin!.Id).Value;
        await externalLogins.AddAsync(link);
        await context.SaveChangesAsync();
    }
}
