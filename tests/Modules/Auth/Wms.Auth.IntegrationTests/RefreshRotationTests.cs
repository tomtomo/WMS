using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Integration test untuk refresh token.
[Collection(PostgresCollection.Name)]
public sealed class RefreshRotationTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Refresh_rotates_and_issues_a_brand_new_pair()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync("rot1");

        var refreshed = await AuthScenarios.RefreshAsync(_provider, refreshToken);

        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.RefreshToken.Should().NotBe(refreshToken, "rotation menghasilkan refresh token baru");
        refreshed.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Reusing_a_rotated_token_is_detected_and_revokes_the_whole_chain()
    {
        var rt1 = await LoginAndGetRefreshTokenAsync("rot2");
        var rt2 = (await AuthScenarios.RefreshAsync(_provider, rt1)).Value.RefreshToken;

        // rt1 sudah dirotasi, jadi penggunaan ulang harus ditolak.
        var reuse = await AuthScenarios.RefreshAsync(_provider, rt1);
        reuse.IsFailure.Should().BeTrue();
        reuse.Error.Code.Should().Be("auth.refresh_token_reused");

        // rt2 ikut ditolak karena masih berada dalam rotation chain yang sama.
        (await AuthScenarios.RefreshAsync(_provider, rt2)).IsFailure
            .Should().BeTrue("chain-revoke mematikan sesi terkini juga");
    }

    [Fact]
    public async Task An_unknown_refresh_token_is_invalid()
    {
        var result = await AuthScenarios.RefreshAsync(_provider, "not-a-real-token");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.refresh_token_invalid");
    }

    [Fact]
    public async Task A_logged_out_token_can_no_longer_be_refreshed()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync("rot3");

        (await AuthScenarios.LogoutAsync(_provider, refreshToken)).IsSuccess.Should().BeTrue();

        (await AuthScenarios.RefreshAsync(_provider, refreshToken)).IsFailure
            .Should().BeTrue("token yang di-logout (revoked) tidak bisa refresh");
    }

    private async Task<string> LoginAndGetRefreshTokenAsync(string username)
    {
        await AuthScenarios.CreateUserAsync(_provider, username, Password);
        var login = await AuthScenarios.LoginAsync(_provider, username, Password);
        return login.Value.RefreshToken;
    }
}
