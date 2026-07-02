using Wms.BuildingBlocks.Domain.Primitives;

namespace Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;

// Test double — value object dengan komponen identik Money, untuk test dua tipe VO beda tak pernah equal.
public sealed class Weight : ValueObject
{
    public Weight(decimal amount, string unit)
    {
        Amount = amount;
        Unit = unit;
    }

    public decimal Amount { get; }

    public string Unit { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Unit;
    }
}
