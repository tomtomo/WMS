using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Authorization.IntegrationTests.TestSupport;

// Test ICurrentUser untuk driving EF global query filter warehouse
internal sealed class ScopedCurrentUser : ICurrentUser
{
    private readonly Guid[] _warehouses;
    private readonly bool _bypass;

    private ScopedCurrentUser(bool bypass, Guid[] warehouses)
    {
        _bypass = bypass;
        _warehouses = warehouses;
    }

    public string UserId => _bypass ? ICurrentUser.SystemActor : "scoped-user";

    public bool IsAuthenticated => !_bypass;

    public IReadOnlyCollection<Guid> AssignedWarehouseIds => _warehouses;

    public bool CanBypassWarehouseScope => _bypass;

    // User terautentikasi yang ter scope ke warehouse tertentu.
    public static ScopedCurrentUser ScopedTo(params Guid[] warehouses) => new(bypass: false, warehouses);

    // Aktor SYSTEM/background: bypass scope (lihat semua).
    public static ScopedCurrentUser System() => new(bypass: true, []);
}
