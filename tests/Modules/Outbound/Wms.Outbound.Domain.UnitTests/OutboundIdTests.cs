using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

// Test Typed ID Outbound
public sealed class OutboundIdTests
{
    [Fact]
    public void OutboundOrderId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = OutboundOrderId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void OutboundOrderId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = OutboundOrderId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void WaveId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = WaveId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void WaveId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = WaveId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void PickingTaskId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = PickingTaskId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void PickingTaskId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = PickingTaskId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void Ids_of_the_same_type_and_value_are_equal()
    {
        var value = Guid.NewGuid();

        OutboundOrderId.Create(value).Value.Should().Be(OutboundOrderId.Create(value).Value);
    }

    [Fact]
    public void Ids_of_different_types_with_the_same_value_are_not_equal()
    {
        var value = Guid.NewGuid();

        var orderId = OutboundOrderId.Create(value).Value;
        var waveId = WaveId.Create(value).Value;

        orderId.Equals(waveId).Should().BeFalse();
    }
}
