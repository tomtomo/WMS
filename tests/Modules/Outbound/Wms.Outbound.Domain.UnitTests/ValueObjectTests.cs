using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Uom_create_succeeds_and_trims_whitespace()
    {
        var result = Uom.Create("  CARTON ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("CARTON");
    }

    [Fact]
    public void Uom_create_rejects_blank_as_invalid()
    {
        var result = Uom.Create("   ");

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("uom.value_required");
    }

    [Fact]
    public void ShipTo_create_succeeds_with_recipient_and_address()
    {
        var result = ShipTo.Create("  Toko Tom ", " Jl. Merdeka 1 ", " Jakarta ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Recipient.Should().Be("Toko Tom");
        result.Value.AddressLine.Should().Be("Jl. Merdeka 1");
        result.Value.City.Should().Be("Jakarta");
    }

    [Fact]
    public void ShipTo_create_rejects_a_blank_recipient()
    {
        ShipTo.Create(" ", "Jl. Merdeka 1", "Jakarta").Error.Code.Should().Be("ship_to.recipient_required");
    }

    [Fact]
    public void ShipTo_create_rejects_a_blank_address_line()
    {
        ShipTo.Create("Toko Tom", " ", "Jakarta").Error.Code.Should().Be("ship_to.address_required");
    }

    [Fact]
    public void ShipTo_create_rejects_a_blank_city()
    {
        ShipTo.Create("Toko Tom", "Jl. Merdeka 1", " ").Error.Code.Should().Be("ship_to.city_required");
    }

    [Fact]
    public void OrderLine_create_starts_pending_with_zero_allocated()
    {
        var uom = Uom.Create("CARTON").Value;

        var result = OrderLine.Create("  SKU-MILK ", 10m, uom);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sku.Should().Be("SKU-MILK");
        result.Value.Qty.Should().Be(10m);
        result.Value.Uom.Should().Be(uom);
        result.Value.AllocatedQty.Should().Be(0m);
        result.Value.AllocationStatus.Should().Be(AllocationStatus.Pending);
    }

    [Fact]
    public void OrderLine_create_rejects_a_blank_sku()
    {
        var uom = Uom.Create("CARTON").Value;

        OrderLine.Create(" ", 10m, uom).Error.Code.Should().Be("order_line.sku_required");
    }

    [Fact]
    public void OrderLine_create_rejects_a_non_positive_qty()
    {
        var uom = Uom.Create("CARTON").Value;

        OrderLine.Create("SKU-MILK", 0m, uom).Error.Code.Should().Be("order_line.qty_invalid");
    }

    [Fact]
    public void OrderLine_create_guards_against_a_missing_uom()
    {
        var act = () => OrderLine.Create("SKU-MILK", 10m, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CancelReason_create_succeeds_and_trims_whitespace()
    {
        CancelReason.Create("  wave nol-terpenuhi ").Value.Value.Should().Be("wave nol-terpenuhi");
    }

    [Fact]
    public void CancelReason_create_rejects_blank_as_invalid()
    {
        CancelReason.Create(" ").Error.Code.Should().Be("cancel_reason.value_required");
    }
}
