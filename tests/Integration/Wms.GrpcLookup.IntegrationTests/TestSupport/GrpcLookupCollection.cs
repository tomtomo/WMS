using Xunit;

namespace Wms.GrpcLookup.IntegrationTests.TestSupport;

[CollectionDefinition(Name)]
public sealed class GrpcLookupCollection : ICollectionFixture<GrpcLookupFixture>
{
    public const string Name = "grpc-lookup";
}
