using Wms.BuildingBlocks.Domain.Primitives;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — value object Money.
public sealed class Money : ValueObject
{
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
