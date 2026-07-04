using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.Events;
using Wms.Inventory.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

// Terbentuknya balance via GRConfirmed
public sealed class StockCreateTests
{
    [Fact]
    public void CreateOnHand_snapshots_the_balance_in_on_hand_state()
    {
        var id = StockMother.NewStockId();

        var result = Stock.CreateOnHand(
            id,
            StockMother.MilkSku,
            StockMother.ReceivingLocation,
            StockMother.BatchOf(),
            StockMother.ExpiryOf(),
            StockMother.QtyOf(100m),
            StockMother.SourceGrId);

        result.IsSuccess.Should().BeTrue();
        var stock = result.Value;
        stock.Id.Should().Be(id);
        stock.Status.Should().Be(StockStatus.OnHand);
        stock.Sku.Should().Be(StockMother.MilkSku);
        stock.LocationId.Should().Be(StockMother.ReceivingLocation);
        stock.Batch.Should().Be(StockMother.BatchOf());
        stock.Expiry.Should().Be(StockMother.ExpiryOf());
        stock.Qty.Should().Be(100m);
        stock.SourceGrId.Should().Be(StockMother.SourceGrId);
    }

    [Fact]
    public void CreateOnHand_raises_stock_created_with_the_on_hand_state()
    {
        var stock = StockMother.OnHand();

        stock.DomainEvents.OfType<StockCreated>().Should().ContainSingle()
            .Which.Status.Should().Be(StockStatus.OnHand);
    }

    [Fact]
    public void CreateQuarantine_snapshots_the_balance_in_quarantine_state()
    {
        var result = Stock.CreateQuarantine(
            StockMother.NewStockId(),
            StockMother.MilkSku,
            StockMother.QuarantineLocation,
            StockMother.BatchOf(),
            StockMother.ExpiryOf(),
            StockMother.QtyOf(5m),
            StockMother.SourceGrId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(StockStatus.Quarantine);
    }

    [Fact]
    public void CreateQuarantine_raises_stock_created_with_the_quarantine_state()
    {
        var stock = StockMother.Quarantine();

        stock.DomainEvents.OfType<StockCreated>().Should().ContainSingle()
            .Which.Status.Should().Be(StockStatus.Quarantine);
    }

    [Fact]
    public void CreateOnHand_rejects_an_empty_source_goods_receipt_as_invalid()
    {
        var result = Stock.CreateOnHand(
            StockMother.NewStockId(),
            StockMother.MilkSku,
            StockMother.ReceivingLocation,
            StockMother.BatchOf(),
            StockMother.ExpiryOf(),
            StockMother.QtyOf(100m),
            Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("stock.source_gr_required");
    }

    [Fact]
    public void A_balance_follows_the_auditable_convention()
    {
        StockMother.OnHand().Should().BeAssignableTo<IAuditable>();
    }
}
