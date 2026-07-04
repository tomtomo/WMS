using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Xunit;

namespace Wms.Contracts.Tests;

// sealed record, koleksi IReadOnlyList, nol setter mutable, tidak ada metadata envelope.
public sealed class ImmutabilityTests
{
    private static readonly string[] _envelopeMetadata = ["eventid", "occurredat", "traceparent", "tracestate"];

    [Fact]
    public void Every_contract_type_is_a_sealed_record()
    {
        foreach (var type in ContractCatalog.RecordTypes)
        {
            type.IsSealed.Should().BeTrue($"{type.Name} wajib sealed");
            IsRecord(type).Should().BeTrue($"{type.Name} wajib record (value equality)");
        }
    }

    [Fact]
    public void No_contract_property_has_a_public_mutable_setter()
    {
        foreach (var type in ContractCatalog.RecordTypes)
        {
            foreach (var property in InstanceProperties(type))
            {
                HasMutableSetter(property).Should().BeFalse(
                    $"{type.Name}.{property.Name} wajib init-only (immutable), bukan setter mutable");
            }
        }
    }

    [Fact]
    public void Every_collection_property_is_an_immutable_read_only_list()
    {
        foreach (var type in ContractCatalog.RecordTypes)
        {
            foreach (var property in InstanceProperties(type).Where(IsCollection))
            {
                property.PropertyType.IsArray.Should().BeFalse(
                    $"{type.Name}.{property.Name} jangan array telanjang (mutable)");
                IsReadOnlyList(property.PropertyType).Should().BeTrue(
                    $"{type.Name}.{property.Name} wajib IReadOnlyList<T>, bukan List<T>/array");
            }
        }
    }

    [Fact]
    public void No_event_carries_envelope_metadata_in_its_payload()
    {
        foreach (var eventType in ContractCatalog.EventTypes)
        {
            var offending = InstanceProperties(eventType)
                .Select(property => property.Name)
                .Where(name => _envelopeMetadata.Contains(name.ToLowerInvariant()))
                .ToList();

            offending.Should().BeEmpty(
                $"{eventType.Name}: metadata (eventId/occurredAt/traceparent) hidup di MessageEnvelope, bukan payload");
        }
    }

    private static IEnumerable<PropertyInfo> InstanceProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.Name != "EqualityContract");

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;

    private static bool HasMutableSetter(PropertyInfo property)
    {
        if (property.SetMethod is not { IsPublic: true } setter)
        {
            return false;
        }

        return !setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(modifier => modifier == typeof(IsExternalInit));
    }

    private static bool IsCollection(PropertyInfo property) =>
        property.PropertyType != typeof(string)
        && typeof(IEnumerable).IsAssignableFrom(property.PropertyType);

    private static bool IsReadOnlyList(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
}
