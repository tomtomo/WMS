using AwesomeAssertions;
using Wms.Inventory.Domain.Allocation;
using Wms.Inventory.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

// FEFO
public sealed class FefoSelectorTests
{
    [Fact]
    public void Order_places_the_nearest_expiry_batch_first()
    {
        var far = StockMother.AvailableWith(new DateOnly(2027, 1, 15));
        var near = StockMother.AvailableWith(new DateOnly(2026, 6, 30));
        var mid = StockMother.AvailableWith(new DateOnly(2026, 12, 31));

        var ordered = FefoSelector.Order([far, mid, near]);

        ordered.Select(stock => stock.Expiry.Value).Should().ContainInOrder(
            new DateOnly(2026, 6, 30),
            new DateOnly(2026, 12, 31),
            new DateOnly(2027, 1, 15));
    }

    [Fact]
    public void Order_breaks_ties_on_batch_deterministically()
    {
        var expiry = new DateOnly(2026, 12, 31);
        var batchB = StockMother.AvailableWith(expiry, "LOT-B");
        var batchA = StockMother.AvailableWith(expiry, "LOT-A");

        var ordered = FefoSelector.Order([batchB, batchA]);

        ordered.Select(stock => stock.Batch.Value).Should().ContainInOrder("LOT-A", "LOT-B");
    }

    [Fact]
    public void Order_does_not_mutate_the_input_sequence()
    {
        var first = StockMother.AvailableWith(new DateOnly(2027, 1, 15));
        var second = StockMother.AvailableWith(new DateOnly(2026, 6, 30));
        var input = new[] { first, second };

        FefoSelector.Order(input);

        input.Should().ContainInOrder(first, second);
    }
}
