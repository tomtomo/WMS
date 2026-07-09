using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.Auth.Infrastructure.Seed;
using Wms.Auth.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// REST /v1 — camelCase
[Collection(PostgresCollection.Name)]
public sealed class RestApiTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Login_returns_200_with_camel_case_tokens()
    {
        var response = await _client.PostAsJsonAsync(
            "/v1/login",
            new { username = AuthSeeder.DefaultAdminUsername, password = AuthSeeder.DefaultAdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"accessToken\"").And.Contain("\"refreshToken\"", "JSON minimal-API camelCase");
    }

    [Fact]
    public async Task Login_with_a_blank_username_returns_400_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/v1/login", new { username = string.Empty, password = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_a_problem_details_400()
    {
        var response = await _client.PostAsJsonAsync(
            "/v1/login",
            new { username = AuthSeeder.DefaultAdminUsername, password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_role_returns_201_with_a_camel_case_body()
    {
        var response = await _client.PostAsJsonAsync(
            "/v1/roles",
            new { code = "Auditor", name = "Auditor", permissionIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("roleId").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Creating_a_role_with_a_duplicate_code_returns_409()
    {
        var role = new { code = "DupRole", name = "Dup", permissionIds = Array.Empty<Guid>() };
        (await _client.PostAsJsonAsync("/v1/roles", role)).StatusCode.Should().Be(HttpStatusCode.Created);

        (await _client.PostAsJsonAsync("/v1/roles", role)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_permission_catalog_endpoint_returns_the_seeded_codes()
    {
        var response = await _client.GetAsync("/v1/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Auth.ManageUser");
    }
}
