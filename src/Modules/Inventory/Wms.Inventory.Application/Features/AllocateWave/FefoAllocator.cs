namespace Wms.Inventory.Application.Features.AllocateWave;

// Strategi alokasi default FEFO: kandidat sudah terurut expiry terdekat
internal static class FefoAllocator
{
    public static AllocationPlan Allocate(decimal requestedQty, IReadOnlyList<AllocationCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var claims = new List<PlannedClaim>();
        var remaining = requestedQty;
        foreach (var candidate in candidates)
        {
            if (remaining <= 0m)
            {
                break;
            }

            if (candidate.AvailableQty <= 0m)
            {
                continue;
            }

            var take = Math.Min(remaining, candidate.AvailableQty);
            claims.Add(new PlannedClaim(candidate.StockId, take));
            remaining -= take;
        }

        return new AllocationPlan(claims, remaining);
    }
}

// Kandidat alokasi: identitas balance dan qty yang masih bisa diklaim.
internal sealed record AllocationCandidate(Guid StockId, decimal AvailableQty);

// Rencana klaim terhadap satu balance.
internal sealed record PlannedClaim(Guid StockId, decimal Qty);

// Hasil alokasi satu line: klaim dan sisa qty tidak terpenuhi (short, ≥ 0).
internal sealed record AllocationPlan(IReadOnlyList<PlannedClaim> Claims, decimal ShortQty);
