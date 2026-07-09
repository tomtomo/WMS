using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure;
using Wms.Auth.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Integration test untuk alur login.
[Collection(PostgresCollection.Name)]
public sealed class LoginFlowTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Login_with_correct_credentials_issues_an_access_and_refresh_token()
    {
        await AuthScenarios.CreateUserAsync(_provider, "op1", Password);

        var result = await AuthScenarios.LoginAsync(_provider, "op1", Password);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_invalid_and_persists_the_failed_attempt()
    {
        var userId = await AuthScenarios.CreateUserAsync(_provider, "op2", Password);

        var result = await AuthScenarios.LoginAsync(_provider, "op2", "wrong-password");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_credentials");

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await context.Set<User>().FirstAsync(candidate => candidate.Id == UserId.Create(userId).Value);
        user.FailedLoginCount.Should().Be(1, "gagal login wajib persist walau Result gagal (TransactionBehavior skip commit)");

        // Login gagal tetap harus masuk audit log, karena percobaan ini bisa mengubah status lockout user.
        var audits = await context.Set<AuditLogRecord>().Where(record => record.Action == "LoginFailed").ToListAsync();
        audits.Should().ContainSingle().Which.Actor.Should().Be("op2");
    }

    [Fact]
    public async Task Login_of_an_unknown_user_returns_the_same_generic_error()
    {
        var result = await AuthScenarios.LoginAsync(_provider, "ghost", Password);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_credentials");
    }

    [Fact]
    public async Task Five_failed_logins_lock_the_account_and_reject_even_a_correct_password()
    {
        await AuthScenarios.CreateUserAsync(_provider, "op3", Password);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await AuthScenarios.LoginAsync(_provider, "op3", "wrong-password");
        }

        var result = await AuthScenarios.LoginAsync(_provider, "op3", Password);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.user_locked");
    }

    [Fact]
    public async Task A_disabled_login_is_rejected_distinctly_from_a_locked_one()
    {
        var userId = await AuthScenarios.CreateUserAsync(_provider, "op4", Password);
        await AuthScenarios.DisableUserAsync(_provider, userId);

        var result = await AuthScenarios.LoginAsync(_provider, "op4", Password);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.user_disabled");
        result.Error.Code.Should().NotBe("auth.user_locked");
    }

    [Fact]
    public async Task A_locked_account_auto_unlocks_once_the_cooldown_elapses()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 7, 6, 8, 0, 0, TimeSpan.Zero));
        var provider = AuthTestHost.Build(connectionString, clock);
        await using (provider.ConfigureAwait(false))
        {
            await AuthTestHost.MigrateAsync(provider);
            await AuthScenarios.CreateUserAsync(provider, "op5", Password);
            for (var attempt = 0; attempt < 5; attempt++)
            {
                await AuthScenarios.LoginAsync(provider, "op5", "wrong-password");
            }

            (await AuthScenarios.LoginAsync(provider, "op5", Password)).Error.Code
                .Should().Be("auth.user_locked", "masih dalam cooldown");

            clock.Advance(TimeSpan.FromMinutes(16));

            (await AuthScenarios.LoginAsync(provider, "op5", Password)).IsSuccess
                .Should().BeTrue("cooldown lewat → auto-unlock lalu login sukses");
        }
    }
}
