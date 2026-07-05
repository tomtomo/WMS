using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.MasterData.Domain.UnitTests;

public sealed class WarehouseTests
{
    [Fact]
    public void Create_starts_an_active_warehouse_with_its_details()
    {
        var id = MasterDataMother.NewWarehouseId();

        var result = Warehouse.Create(id, "DC Jakarta Cakung", "Jl. Raya Cakung No. 1");

        result.IsSuccess.Should().BeTrue();
        var warehouse = result.Value;
        warehouse.Id.Should().Be(id);
        warehouse.Name.Should().Be("DC Jakarta Cakung");
        warehouse.Address.Should().Be("Jl. Raya Cakung No. 1");
        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_a_blank_name_as_invalid()
    {
        var result = Warehouse.Create(MasterDataMother.NewWarehouseId(), "  ", "Jl. Raya Cakung No. 1");

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("warehouse.name_required");
    }

    [Fact]
    public void Create_rejects_a_blank_address_as_invalid()
    {
        var result = Warehouse.Create(MasterDataMother.NewWarehouseId(), "DC Jakarta Cakung", " ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("warehouse.address_required");
    }

    [Fact]
    public void Create_raises_no_domain_event_because_master_data_is_read_only_to_core()
    {
        MasterDataMother.AWarehouse().DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void A_warehouse_follows_the_auditable_convention()
    {
        MasterDataMother.AWarehouse().Should().BeAssignableTo<IAuditable>();
    }

    [Fact]
    public void Update_changes_the_editable_details()
    {
        var warehouse = MasterDataMother.AWarehouse();

        var result = warehouse.Update("DC Bandung", "Jl. Soekarno Hatta No. 9");

        result.IsSuccess.Should().BeTrue();
        warehouse.Name.Should().Be("DC Bandung");
        warehouse.Address.Should().Be("Jl. Soekarno Hatta No. 9");
    }

    [Fact]
    public void Update_rejects_a_blank_name_as_invalid()
    {
        var warehouse = MasterDataMother.AWarehouse();

        var result = warehouse.Update(string.Empty, "Jl. Soekarno Hatta No. 9");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("warehouse.name_required");
    }

    [Fact]
    public void Deactivate_soft_deletes_the_warehouse()
    {
        var warehouse = MasterDataMother.AWarehouse();

        var result = warehouse.Deactivate();

        result.IsSuccess.Should().BeTrue();
        warehouse.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_is_idempotent()
    {
        var warehouse = MasterDataMother.AWarehouse();
        warehouse.Deactivate();

        var second = warehouse.Deactivate();

        second.IsSuccess.Should().BeTrue();
        warehouse.IsActive.Should().BeFalse();
    }
}
