using AwesomeAssertions;
using Wms.Platform.Shared.Security;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

public sealed class Argon2idPasswordHasherTests
{
    private readonly Argon2idPasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_verify_round_trips()
    {
        var hash = _hasher.Hash("kata-sandi-kuat-77");

        _hasher.Verify("kata-sandi-kuat-77", hash).Should().BeTrue();
    }

    [Fact]
    public void Wrong_password_fails_verification()
    {
        var hash = _hasher.Hash("kata-sandi-kuat-77");

        _hasher.Verify("kata-sandi-salah", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_is_phc_self_describing_with_owasp_parameters()
    {
        var hash = _hasher.Hash("kata-sandi-kuat-77");

        hash.Should().StartWith("$argon2id$v=19$m=19456,t=2,p=1$");
    }

    [Fact]
    public void Same_password_produces_different_hashes_per_salt()
    {
        var first = _hasher.Hash("kata-sandi-kuat-77");
        var second = _hasher.Hash("kata-sandi-kuat-77");

        second.Should().NotBe(first);
    }

    [Fact]
    public void Malformed_hash_never_verifies()
    {
        _hasher.Verify("apa-saja", "bukan-format-phc").Should().BeFalse();
    }

    [Fact]
    public void Fresh_hash_does_not_need_rehash()
    {
        var hash = _hasher.Hash("kata-sandi-kuat-77");

        _hasher.NeedsRehash(hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_with_outdated_parameters_still_verifies_but_signals_rehash()
    {
        var legacyHasher = new Argon2idPasswordHasher(memoryKibibytes: 8192, iterations: 1, parallelism: 1);
        var legacyHash = legacyHasher.Hash("kata-sandi-kuat-77");

        _hasher.Verify("kata-sandi-kuat-77", legacyHash).Should().BeTrue();
        _hasher.NeedsRehash(legacyHash).Should().BeTrue();
    }
}
