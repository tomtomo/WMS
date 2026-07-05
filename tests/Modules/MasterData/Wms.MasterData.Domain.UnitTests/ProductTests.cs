using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.MasterData.Domain.UnitTests;

public sealed class ProductTests
{
    [Fact]
    public void Create_starts_an_active_product_snapshotting_the_tracking_flags()
    {
        var sku = MasterDataMother.SkuOf("SKU-MILK");

        var result = Product.Create(
            sku,
            name: "Fresh Milk 1L",
            uom: "carton",
            batchTrackingRequired: true,
            expiryTrackingRequired: true,
            qcRequiredOnReceipt: true,
            shelfLifeDays: 30);

        result.IsSuccess.Should().BeTrue();
        var product = result.Value;
        product.Id.Should().Be(sku);
        product.Name.Should().Be("Fresh Milk 1L");
        product.Uom.Should().Be("carton");
        product.BatchTrackingRequired.Should().BeTrue();
        product.ExpiryTrackingRequired.Should().BeTrue();
        product.QcRequiredOnReceipt.Should().BeTrue("flag disimpan");
        product.ShelfLifeDays.Should().Be(30);
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_a_blank_uom_as_invalid()
    {
        var result = Product.Create(MasterDataMother.SkuOf(), "Fresh Milk 1L", " ", false, false, false, null);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("product.uom_required");
    }

    [Fact]
    public void Create_rejects_a_blank_name_as_invalid()
    {
        var result = Product.Create(MasterDataMother.SkuOf(), string.Empty, "carton", false, false, false, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("product.name_required");
    }

    [Fact]
    public void Create_allows_an_absent_shelf_life()
    {
        var result = Product.Create(MasterDataMother.SkuOf(), "Dry Goods", "piece", false, false, false, shelfLifeDays: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ShelfLifeDays.Should().BeNull();
    }

    [Fact]
    public void Create_rejects_a_negative_shelf_life_as_invalid()
    {
        var result = Product.Create(MasterDataMother.SkuOf(), "Fresh Milk 1L", "carton", false, true, false, shelfLifeDays: -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("product.shelf_life_invalid");
    }

    [Fact]
    public void Create_raises_no_domain_event()
    {
        MasterDataMother.AProduct().DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void A_product_follows_the_auditable_convention()
    {
        MasterDataMother.AProduct().Should().BeAssignableTo<IAuditable>();
    }

    [Fact]
    public void Update_changes_the_editable_catalog_fields()
    {
        var product = MasterDataMother.AProduct();

        var result = product.Update("Fresh Milk 2L", "box", batchTrackingRequired: false, expiryTrackingRequired: true, qcRequiredOnReceipt: true, shelfLifeDays: 45);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Fresh Milk 2L");
        product.Uom.Should().Be("box");
        product.BatchTrackingRequired.Should().BeFalse();
        product.QcRequiredOnReceipt.Should().BeTrue();
        product.ShelfLifeDays.Should().Be(45);
    }

    [Fact]
    public void Deactivate_is_idempotent()
    {
        var product = MasterDataMother.AProduct();
        product.Deactivate();

        var second = product.Deactivate();

        second.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();
    }
}
