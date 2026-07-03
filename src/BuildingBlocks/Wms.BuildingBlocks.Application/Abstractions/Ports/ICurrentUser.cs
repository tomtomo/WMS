namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

public interface ICurrentUser
{
    // Aktor saat tak ada user terautentikasi (background worker, event consumer).
    const string SystemActor = "SYSTEM";

    string UserId { get; }

    bool IsAuthenticated { get; }
}
