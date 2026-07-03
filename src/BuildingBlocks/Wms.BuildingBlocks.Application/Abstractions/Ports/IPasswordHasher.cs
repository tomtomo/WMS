namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Hash dan verifikasi password.
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
