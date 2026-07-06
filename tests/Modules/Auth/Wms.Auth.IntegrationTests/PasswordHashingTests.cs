using AwesomeAssertions;
using Wms.Platform.Local.Security;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Test password hashing.
public sealed class PasswordHashingTests
{
    private readonly Argon2idPasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_verify_round_trips_true()
    {
        var hash = _hasher.Hash("P@ssw0rd-123");

        _hasher.Verify("P@ssw0rd-123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_a_wrong_password()
    {
        var hash = _hasher.Hash("P@ssw0rd-123");

        _hasher.Verify("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_is_never_plaintext_and_is_self_describing_owasp()
    {
        var hash = _hasher.Hash("P@ssw0rd-123");

        hash.Should().NotBe("P@ssw0rd-123");
        hash.Should().StartWith("$argon2id$v=19$").And.Contain("m=").And.Contain("t=").And.Contain("p=");
    }

    [Fact]
    public void Two_hashes_of_the_same_password_differ_by_salt()
    {
        _hasher.Hash("same-password").Should().NotBe(_hasher.Hash("same-password"));
    }
}
