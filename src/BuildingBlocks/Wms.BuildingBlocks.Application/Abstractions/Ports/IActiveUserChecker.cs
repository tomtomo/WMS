namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Mengecek apakah user masih aktif.
public interface IActiveUserChecker
{
    Task<bool> IsActiveAsync(string userId, CancellationToken cancellationToken = default);
}
