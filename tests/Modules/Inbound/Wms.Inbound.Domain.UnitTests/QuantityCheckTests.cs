using AwesomeAssertions;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// Test QuantityCheck.Compute
public sealed class QuantityCheckTests
{
    [Fact]
    public void Compute_yields_normal_when_actual_equals_expected()
    {
        var check = QuantityCheck.Compute("SKU-MILK", expectedQty: 100m, actualQty: 100m);

        check.Variance.Should().Be(QuantityVariance.Normal);
    }

    [Fact]
    public void Compute_yields_short_delivery_when_actual_is_below_expected()
    {
        var check = QuantityCheck.Compute("SKU-MILK", expectedQty: 100m, actualQty: 80m);

        check.Variance.Should().Be(QuantityVariance.ShortDelivery);
    }

    [Fact]
    public void Compute_yields_over_delivery_when_actual_is_above_expected()
    {
        var check = QuantityCheck.Compute("SKU-MILK", expectedQty: 100m, actualQty: 120m);

        check.Variance.Should().Be(QuantityVariance.OverDelivery);
    }

    [Fact]
    public void Compute_is_exact_match_so_one_unit_short_is_already_a_variance()
    {
        var check = QuantityCheck.Compute("SKU-MILK", expectedQty: 100m, actualQty: 99m);

        check.Variance.Should().Be(QuantityVariance.ShortDelivery);
    }
}
