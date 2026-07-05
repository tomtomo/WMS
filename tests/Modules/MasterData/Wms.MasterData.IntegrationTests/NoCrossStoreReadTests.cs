using NetArchTest.Rules;
using Wms.MasterData.Api.GrpcServices;
using Wms.MasterData.Grpc.V1;
using Xunit;

namespace Wms.MasterData.IntegrationTests;

public sealed class NoCrossStoreReadTests
{
    [Fact]
    public void Api_assembly_does_not_depend_on_ef_core()
    {
        var result = Types.InAssemblies([typeof(MasterDataLookupService).Assembly])
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Wms.MasterData.Api menyentuh EF Core (harus lewat read-port): {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Grpc_published_language_does_not_depend_on_ef_core()
    {
        var result = Types.InAssemblies([typeof(MasterDataLookup).Assembly])
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Wms.MasterData.Grpc menyentuh EF Core: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
