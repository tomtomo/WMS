namespace Wms.Inventory.Domain.Allocation;

// FEFO (First Expired First Out) — default allocation strategy: urutkan kandidat Available by expiry terdekat
public static class FefoSelector
{
    public static IReadOnlyList<Stock> Order(IEnumerable<Stock> available)
    {
        ArgumentNullException.ThrowIfNull(available);

        return [.. available
            .OrderBy(stock => stock.Expiry.Value)
            .ThenBy(stock => stock.Batch.Value, StringComparer.Ordinal)];
    }
}
