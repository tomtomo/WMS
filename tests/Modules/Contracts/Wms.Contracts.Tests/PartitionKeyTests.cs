using System.Reflection;
using System.Text.Json;
using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Inbound.Contracts;
using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Enums;
using Wms.Outbound.Contracts;
using Xunit;

namespace Wms.Contracts.Tests;

// PartitionKey dipakai sebagai kunci urutan event CoreFlow di transport: session, partition, atau ordering key.
public sealed class PartitionKeyTests
{
    private static readonly Guid _grId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _waveId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _stockId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // Sampel tiap event CoreFlow dengan kunci aliran yang diharapkan: GR per GrId, rantai wave per WaveId, putaway per StockId.
    private static IReadOnlyList<(IIntegrationEvent Event, string ExpectedKey)> CoreFlowSamples =>
    [
        (new GRConfirmed(_grId, Guid.NewGuid(), Guid.NewGuid(), [], []), _grId.ToString()),
        (new StockAllocationCompleted(_waveId, AllocationStatus.FullyAllocated, [], []), _waveId.ToString()),
        (new PutawayCompleted(Guid.NewGuid(), _stockId, "SKU-1", Guid.NewGuid(), null), _stockId.ToString()),
        (new StockRemoved(_waveId, []), _waveId.ToString()),
        (new WaveReleased(_waveId, []), _waveId.ToString()),
        (new PickingCompleted(_waveId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SKU-1", null, 1m, Guid.NewGuid(), null), _waveId.ToString()),
        (new ShipmentDispatched(_waveId), _waveId.ToString()),
    ];

    [Fact]
    public void Samples_cover_every_core_flow_event_in_the_catalog()
    {
        var coreFlowTypes = ContractCatalog.EventTypes.Where(IsCoreFlow).ToList();

        CoreFlowSamples.Select(sample => sample.Event.GetType()).Should().BeEquivalentTo(
            coreFlowTypes,
            "tiap event CoreFlow wajib punya sampel partition key di test ini (non-vacuous)");
    }

    [Fact]
    public void Every_core_flow_event_declares_its_flow_partition_key()
    {
        foreach (var (sample, expectedKey) in CoreFlowSamples)
        {
            sample.Should().BeAssignableTo<IHasPartitionKey>(
                $"{sample.GetType().Name} CoreFlow butuh identitas aliran untuk ordering");
            ((IHasPartitionKey)sample).PartitionKey.Should().Be(
                expectedKey,
                $"{sample.GetType().Name} kunci aliran mengikuti aggregate alur");
        }
    }

    [Fact]
    public void Partition_key_stays_out_of_the_wire_payload()
    {
        foreach (var (sample, _) in CoreFlowSamples)
        {
            var payload = JsonSerializer.Serialize(
                sample,
                sample.GetType(),
                MessageEnvelope.PayloadSerializerOptions);

            payload.Should().NotContainEquivalentOf(
                "partitionKey",
                $"{sample.GetType().Name}: partition key = metadata envelope, bukan payload");
        }
    }

    private static bool IsCoreFlow(Type eventType)
    {
        var field = eventType.GetField(
            "DeliveryClass",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        field.Should().NotBeNull($"{eventType.Name} wajib punya const DeliveryClass");
        return (DeliveryClass)field!.GetValue(null)! == DeliveryClass.CoreFlow;
    }
}
