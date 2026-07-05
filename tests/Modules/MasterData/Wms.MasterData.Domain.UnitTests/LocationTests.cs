using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Domain.Enums;
using Wms.MasterData.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.MasterData.Domain.UnitTests;

public sealed class LocationTests
{
    [Fact]
    public void Create_starts_an_active_location_bound_to_a_warehouse()
    {
        var id = MasterDataMother.NewLocationId();
        var warehouseId = MasterDataMother.NewWarehouseId();

        var result = Location.Create(id, warehouseId, LocationType.QuarantineArea, "QC-A");

        result.IsSuccess.Should().BeTrue();
        var location = result.Value;
        location.Id.Should().Be(id);
        location.WarehouseId.Should().Be(warehouseId);
        location.Type.Should().Be(LocationType.QuarantineArea);
        location.Code.Should().Be("QC-A");
        location.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_a_blank_code_as_invalid()
    {
        var result = Location.Create(
            MasterDataMother.NewLocationId(), MasterDataMother.NewWarehouseId(), LocationType.Rack, " ");

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("location.code_required");
    }

    [Fact]
    public void Create_rejects_an_undefined_location_type_as_invalid()
    {
        var result = Location.Create(
            MasterDataMother.NewLocationId(), MasterDataMother.NewWarehouseId(), (LocationType)999, "RACK-01");

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("location.type_invalid");
    }

    [Fact]
    public void Create_raises_no_domain_event()
    {
        MasterDataMother.ALocation().DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void A_location_follows_the_auditable_convention()
    {
        MasterDataMother.ALocation().Should().BeAssignableTo<IAuditable>();
    }

    [Fact]
    public void Update_changes_the_type_and_code()
    {
        var location = MasterDataMother.ALocation();

        var result = location.Update(LocationType.StagingArea, "STG-2");

        result.IsSuccess.Should().BeTrue();
        location.Type.Should().Be(LocationType.StagingArea);
        location.Code.Should().Be("STG-2");
    }

    [Fact]
    public void Deactivate_is_idempotent()
    {
        var location = MasterDataMother.ALocation();
        location.Deactivate();

        var second = location.Deactivate();

        second.IsSuccess.Should().BeTrue();
        location.IsActive.Should().BeFalse();
    }
}
