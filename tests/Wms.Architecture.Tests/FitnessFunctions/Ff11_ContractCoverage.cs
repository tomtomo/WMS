using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Messaging;
using Xunit;
using YamlDotNet.Serialization;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#11 — tiap integration event published (*.Contracts, IIntegrationEvent) wajib punya channel di katalog
// AsyncAPI (asyncapi.yaml, address = LogicalName).
public sealed class Ff11_ContractCoverage
{
    [Fact]
    public void Every_published_event_has_asyncapi_channel()
    {
        var eventLogicalNames = PublishedEventLogicalNames();
        if (eventLogicalNames.Count == 0)
        {
            return;
        }

        var catalogAddresses = AsyncApiChannelAddresses();
        var uncovered = eventLogicalNames.Where(name => !catalogAddresses.Contains(name)).ToList();

        uncovered.Should().BeEmpty("tiap integration event wajib punya channel AsyncAPI (FF#11)");
    }

    private static IReadOnlyCollection<string> PublishedEventLogicalNames() =>
    [
        .. ArchitectureFixture.ContractsAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IIntegrationEvent).IsAssignableFrom(type))
            .Select(IntegrationEventLogicalName.Resolve),
    ];

    private static IReadOnlySet<string> AsyncApiChannelAddresses()
    {
        var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        var addresses = new HashSet<string>(StringComparer.Ordinal);
        foreach (var catalog in SourceScan.AsyncApiCatalogs())
        {
            var document = deserializer.Deserialize<AsyncApiDocument>(File.ReadAllText(catalog));
            if (document?.Channels is null)
            {
                continue;
            }

            addresses.UnionWith(document.Channels.Values
                .Where(channel => !string.IsNullOrEmpty(channel?.Address))
                .Select(channel => channel!.Address!));
        }

        return addresses;
    }

    // Bentuk minimal AsyncAPI 3.0
    private sealed class AsyncApiDocument
    {
        [YamlMember(Alias = "channels")]
        public Dictionary<string, AsyncApiChannel>? Channels { get; set; }
    }

    private sealed class AsyncApiChannel
    {
        [YamlMember(Alias = "address")]
        public string? Address { get; set; }
    }
}
