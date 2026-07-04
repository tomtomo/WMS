using System.Reflection;
using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Messaging;
using Xunit;
using YamlDotNet.Serialization;

namespace Wms.Contracts.Tests;

// Katalog AsyncAPI konsisten dengan kode.
public sealed class AsyncApiCatalogTests
{
    private static readonly IReadOnlyDictionary<string, AsyncApiChannel> _channels = LoadChannelsByAddress();

    [Fact]
    public void Every_published_event_has_a_channel_addressed_by_its_logical_name()
    {
        foreach (var eventType in ContractCatalog.EventTypes)
        {
            var logicalName = IntegrationEventLogicalName.Resolve(eventType);
            _channels.Keys.Should().Contain(
                logicalName,
                $"{eventType.Name}: LogicalName '{logicalName}' wajib jadi address channel AsyncAPI (FF#11 nilai)");
        }
    }

    [Fact]
    public void Channel_delivery_class_matches_the_declared_rails_in_code()
    {
        foreach (var eventType in ContractCatalog.EventTypes)
        {
            var logicalName = IntegrationEventLogicalName.Resolve(eventType);
            if (!_channels.TryGetValue(logicalName, out var channel))
            {
                continue;
            }

            channel.DeliveryClass.Should().BeEquivalentTo(
                DeclaredRails(eventType),
                $"{eventType.Name}: x-deliveryClass katalog wajib cocok rail yang di-deklarasi kode");
        }
    }

    private static IReadOnlyList<string> DeclaredRails(Type eventType)
    {
        var multi = eventType.GetField("DeliveryClasses", BindingFlags.Public | BindingFlags.Static);
        if (multi?.GetValue(null) is IEnumerable<DeliveryClass> rails)
        {
            return [.. rails.Select(rail => rail.ToString())];
        }

        var single = eventType.GetField(
            "DeliveryClass",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return single?.GetValue(null) is DeliveryClass only ? [only.ToString()] : [];
    }

    private static IReadOnlyDictionary<string, AsyncApiChannel> LoadChannelsByAddress()
    {
        var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
        var document = deserializer.Deserialize<AsyncApiDocument>(File.ReadAllText(FindCatalog()));

        return document.Channels!.Values
            .Where(channel => !string.IsNullOrEmpty(channel.Address))
            .ToDictionary(channel => channel.Address!, StringComparer.Ordinal);
    }

    private static string FindCatalog()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "asyncapi.yaml")))
        {
            directory = directory.Parent;
        }

        return directory is not null
            ? Path.Combine(directory.FullName, "asyncapi.yaml")
            : throw new InvalidOperationException("asyncapi.yaml tidak ditemukan di jalur induk test host.");
    }

    private sealed class AsyncApiDocument
    {
        [YamlMember(Alias = "channels")]
        public Dictionary<string, AsyncApiChannel>? Channels { get; set; }
    }

    private sealed class AsyncApiChannel
    {
        [YamlMember(Alias = "address")]
        public string? Address { get; set; }

        [YamlMember(Alias = "x-deliveryClass")]
        public List<string> DeliveryClass { get; set; } = [];
    }
}
