using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure;
using Wms.Auth.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Test penyimpanan dan pencarian akun eksternal, termasuk aturan unik untuk provider dan subject.
[Collection(PostgresCollection.Name)]
public sealed class UserExternalLoginPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task A_linked_external_identity_resolves_to_its_user()
    {
        var userId = await AuthScenarios.CreateUserAsync(_provider, "op1", Password);
        await LinkAsync(ExternalLoginProviders.Entra, "oid-abc", userId);

        using var scope = _provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserExternalLoginRepository>();

        var resolved = await repository.FindUserIdAsync(ExternalLoginProviders.Entra, "oid-abc");

        resolved.Should().NotBeNull();
        resolved!.Value.Should().Be(userId);
        (await repository.ExistsAsync(ExternalLoginProviders.Entra, "oid-abc")).Should().BeTrue();
        (await repository.FindUserIdAsync(ExternalLoginProviders.Entra, "oid-unknown")).Should().BeNull();
    }

    [Fact]
    public async Task The_same_provider_subject_cannot_be_linked_twice()
    {
        var firstUser = await AuthScenarios.CreateUserAsync(_provider, "op2", Password);
        var secondUser = await AuthScenarios.CreateUserAsync(_provider, "op3", Password);
        await LinkAsync(ExternalLoginProviders.Entra, "oid-dup", firstUser);

        var relink = async () => await LinkAsync(ExternalLoginProviders.Entra, "oid-dup", secondUser);

        await relink.Should().ThrowAsync<DbUpdateException>("index unik (provider, subject) menahan duplikasi");
    }

    private async Task LinkAsync(string provider, string subject, Guid userId)
    {
        using var scope = _provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserExternalLoginRepository>();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var link = UserExternalLogin.Link(
            UserExternalLoginId.Create(Guid.NewGuid()).Value,
            provider,
            subject,
            UserId.Create(userId).Value).Value;

        await repository.AddAsync(link);
        await context.SaveChangesAsync();
    }
}
