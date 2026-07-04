using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.UnitTests.TestData;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// SPV input GR: state InProgress.
public sealed class GoodsReceiptCreateTests
{
    [Fact]
    public void Create_succeeds_with_in_progress_state_and_snapshotted_fields()
    {
        var id = GoodsReceiptMother.NewId();
        var expected = GoodsReceiptMother.Expected();

        var result = GoodsReceipt.Create(
            id,
            GoodsReceiptMother.PoRef,
            GoodsReceiptMother.SupplierId,
            GoodsReceiptMother.WarehouseId,
            GoodsReceiptMother.Dock(),
            [expected]);

        result.IsSuccess.Should().BeTrue();
        var goodsReceipt = result.Value;
        goodsReceipt.Id.Should().Be(id);
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.InProgress);
        goodsReceipt.PoRef.Should().Be(GoodsReceiptMother.PoRef);
        goodsReceipt.SupplierId.Should().Be(GoodsReceiptMother.SupplierId);
        goodsReceipt.WarehouseId.Should().Be(GoodsReceiptMother.WarehouseId);
        goodsReceipt.ExpectedLines.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public void Create_does_not_raise_any_domain_event()
    {
        GoodsReceiptMother.InProgress().DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Aggregate_root_follows_the_auditable_convention()
    {
        GoodsReceiptMother.InProgress().Should().BeAssignableTo<IAuditable>();
    }

    [Fact]
    public void Create_rejects_an_empty_supplier_as_invalid()
    {
        var result = GoodsReceipt.Create(
            GoodsReceiptMother.NewId(),
            GoodsReceiptMother.PoRef,
            Guid.Empty,
            GoodsReceiptMother.WarehouseId,
            GoodsReceiptMother.Dock(),
            [GoodsReceiptMother.Expected()]);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("goods_receipt.supplier_required");
    }

    [Fact]
    public void Create_rejects_an_empty_warehouse_as_invalid()
    {
        var result = GoodsReceipt.Create(
            GoodsReceiptMother.NewId(),
            GoodsReceiptMother.PoRef,
            GoodsReceiptMother.SupplierId,
            Guid.Empty,
            GoodsReceiptMother.Dock(),
            [GoodsReceiptMother.Expected()]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("goods_receipt.warehouse_required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_po_ref_as_invalid(string poRef)
    {
        var result = GoodsReceipt.Create(
            GoodsReceiptMother.NewId(),
            poRef,
            GoodsReceiptMother.SupplierId,
            GoodsReceiptMother.WarehouseId,
            GoodsReceiptMother.Dock(),
            [GoodsReceiptMother.Expected()]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("goods_receipt.po_ref_required");
    }

    [Fact]
    public void Create_rejects_empty_expected_lines_as_invalid()
    {
        var result = GoodsReceipt.Create(
            GoodsReceiptMother.NewId(),
            GoodsReceiptMother.PoRef,
            GoodsReceiptMother.SupplierId,
            GoodsReceiptMother.WarehouseId,
            GoodsReceiptMother.Dock(),
            []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("goods_receipt.expected_lines_required");
    }

    [Fact]
    public void Create_rejects_duplicated_expected_skus_as_invalid()
    {
        var result = GoodsReceipt.Create(
            GoodsReceiptMother.NewId(),
            GoodsReceiptMother.PoRef,
            GoodsReceiptMother.SupplierId,
            GoodsReceiptMother.WarehouseId,
            GoodsReceiptMother.Dock(),
            [GoodsReceiptMother.Expected(qty: 40m), GoodsReceiptMother.Expected(qty: 60m)]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("goods_receipt.expected_sku_duplicated");
    }

    [Fact]
    public void Expected_lines_are_a_snapshot_that_later_mutation_of_the_source_cannot_change()
    {
        var source = new List<ExpectedLine> { GoodsReceiptMother.Expected() };
        var goodsReceipt = GoodsReceipt.Create(
            GoodsReceiptMother.NewId(),
            GoodsReceiptMother.PoRef,
            GoodsReceiptMother.SupplierId,
            GoodsReceiptMother.WarehouseId,
            GoodsReceiptMother.Dock(),
            source).Value;

        source.Add(GoodsReceiptMother.Expected(sku: "SKU-CHEESE"));
        source.Clear();

        goodsReceipt.ExpectedLines.Should().ContainSingle().Which.Sku.Should().Be(GoodsReceiptMother.Sku);
    }
}
