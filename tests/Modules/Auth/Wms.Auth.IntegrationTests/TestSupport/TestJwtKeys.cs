using System.Security.Cryptography;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Key pair RSA untuk test
internal static class TestJwtKeys
{
    public const string SigningKeySecretName = "jwt-signing-key";

    public const string Issuer = "wms-auth-tests";

    public const string Audience = "wms-tests";

    private static readonly (string Private, string Public) _keys = CreateKeys();

    public static string PrivateKeyPem => _keys.Private;

    public static string PublicKeyPem => _keys.Public;

    private static (string Private, string Public) CreateKeys()
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }
}
