using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Application.Features.EntraLogin;
using Wms.Auth.Application.Features.LinkExternalLogin;
using Wms.Auth.Application.Features.Login;
using Wms.Auth.Domain;
using Wms.Auth.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Test login melalui Entra menggunakan token dan JWKS khusus test hingga menghasilkan JWT internal untuk akun yang sudah ditautkan.
[Collection(PostgresCollection.Name)]
public sealed class EntraLoginFlowTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "P@ssw0rd-123";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = AuthTestHost.BuildWithEntra(connectionString, TestEntraTokens.ConfigurationManager());
        await AuthTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task A_linked_entra_identity_logs_in_and_receives_internal_tokens()
    {
        var userId = await AuthScenarios.CreateUserAsync(_provider, "op1", Password);
        await LinkEntraAsync(userId, "oid-abc");

        var result = await SendEntraLoginAsync(TestEntraTokens.Mint("oid-abc"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_unlinked_entra_identity_is_rejected_fail_secure()
    {
        var result = await SendEntraLoginAsync(TestEntraTokens.Mint("oid-unlinked"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.entra_not_linked");
    }

    [Fact]
    public async Task An_invalid_id_token_is_rejected()
    {
        var result = await SendEntraLoginAsync("not-a-real-jwt");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.entra_token_invalid");
    }

    [Fact]
    public async Task A_disabled_linked_user_is_rejected_distinctly_from_unlinked()
    {
        var userId = await AuthScenarios.CreateUserAsync(_provider, "op2", Password);
        await LinkEntraAsync(userId, "oid-disabled");
        await AuthScenarios.DisableUserAsync(_provider, userId);

        var result = await SendEntraLoginAsync(TestEntraTokens.Mint("oid-disabled"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.user_disabled");
    }

    private async Task<Result<LoginResponse>> SendEntraLoginAsync(string idToken)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EntraLoginCommand(idToken));
    }

    private async Task LinkEntraAsync(Guid userId, string subject)
    {
        using var scope = _provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new LinkExternalLoginCommand(userId, ExternalLoginProviders.Entra, subject));
    }
}
