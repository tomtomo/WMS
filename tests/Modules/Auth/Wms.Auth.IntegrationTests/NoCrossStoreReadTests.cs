using NetArchTest.Rules;
using Wms.Auth.Api.GrpcServices;
using Wms.Auth.Grpc.V1;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// .Api dan .Grpc tidak menyentuh EF Core (read lewat read port).
public sealed class NoCrossStoreReadTests
{
    [Fact]
    public void Api_assembly_does_not_depend_on_ef_core()
    {
        var result = Types.InAssemblies([typeof(AuthLookupService).Assembly])
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Wms.Auth.Api menyentuh EF Core (harus lewat read-port): {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Grpc_published_language_does_not_depend_on_ef_core()
    {
        var result = Types.InAssemblies([typeof(AuthLookup).Assembly])
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Wms.Auth.Grpc menyentuh EF Core: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
