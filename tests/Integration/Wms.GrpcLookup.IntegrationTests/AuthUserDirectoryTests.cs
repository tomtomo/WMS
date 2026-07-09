using AwesomeAssertions;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Domain;
using Wms.Auth.Grpc.V1;
using Wms.Auth.Infrastructure;
using Wms.GrpcLookup.IntegrationTests.TestSupport;
using Wms.Notifications.UserDirectory;
using Xunit;

namespace Wms.GrpcLookup.IntegrationTests;

// Memastikan UserDirectory Notifications membaca anggota role dan recipient lewat gRPC Auth.
[Collection(GrpcLookupCollection.Name)]
public sealed class AuthUserDirectoryTests(GrpcLookupFixture fixture) : IAsyncLifetime
{
    private WebApplication _authHost = null!;
    private GrpcChannel _channel = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync("auth");
        _authHost = await AuthLookupHost.StartAsync(connectionString);
        _channel = GrpcChannel.ForAddress(
            _authHost.GetTestClient().BaseAddress!,
            new GrpcChannelOptions { HttpHandler = _authHost.GetTestServer().CreateHandler() });
    }

    public async Task DisposeAsync()
    {
        _channel.Dispose();
        await _authHost.DisposeAsync();
    }

    [Fact]
    public async Task User_directory_resolves_role_members_and_recipient_via_real_grpc()
    {
        User seededUser;
        using (var scope = _authHost.Services.CreateScope())
        {
            var users = await scope.ServiceProvider.GetRequiredService<AuthDbContext>()
                .Set<User>().AsNoTracking().ToListAsync();
            seededUser = users.First(user => user.RoleIds.Count > 0);
        }

        var roleId = seededUser.RoleIds[0];
        var directory = new AuthGrpcUserDirectory(new AuthLookup.AuthLookupClient(_channel));

        var members = await directory.GetUsersInRoleAsync(roleId);
        members.Should().Contain(seededUser.Id.Value, "Role→member di-resolve lintas-proses via gRPC nyata");

        var recipient = await directory.GetRecipientAsync(seededUser.Id.Value);
        recipient.Should().NotBeNull();
        recipient!.Email.Should().Be(seededUser.Email);
    }
}
