using Wms.Inbound.Application.Abstractions;

namespace Wms.Inbound.IntegrationTests.TestSupport;

// Fake master data
internal sealed class FakeProductReader : IProductReader
{
    private readonly HashSet<string> _unknownSkus = new(StringComparer.Ordinal);

    public void MarkUnknown(string sku) => _unknownSkus.Add(sku);

    public Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(!_unknownSkus.Contains(sku));
}
