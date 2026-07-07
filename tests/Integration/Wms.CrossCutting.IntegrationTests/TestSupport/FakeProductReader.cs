using Wms.Inbound.Application.Abstractions;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

internal sealed class FakeProductReader : IProductReader
{
    public Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
