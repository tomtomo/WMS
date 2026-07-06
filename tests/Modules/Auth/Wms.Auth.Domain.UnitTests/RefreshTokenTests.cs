using AwesomeAssertions;
using Wms.Auth.Domain.UnitTests.TestData;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Auth.Domain.UnitTests;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset _issuedAt = new(2026, 7, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _expiresAt = new(2026, 7, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_creates_an_active_token_before_expiry()
    {
        var token = AuthMother.ARefreshToken(issuedAt: _issuedAt, expiresAt: _expiresAt);

        token.IsActive(_issuedAt).Should().BeTrue();
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public void A_token_is_inactive_exactly_at_the_expiry_boundary()
    {
        var token = AuthMother.ARefreshToken(issuedAt: _issuedAt, expiresAt: _expiresAt);

        token.IsActive(_expiresAt).Should().BeFalse("now == expiresAt harus dianggap kedaluwarsa");
    }

    [Fact]
    public void A_token_is_inactive_after_expiry()
    {
        var token = AuthMother.ARefreshToken(issuedAt: _issuedAt, expiresAt: _expiresAt);

        token.IsActive(_expiresAt.AddTicks(1)).Should().BeFalse();
    }

    [Fact]
    public void Rotate_revokes_the_old_token_and_links_its_replacement()
    {
        var token = AuthMother.ARefreshToken(issuedAt: _issuedAt, expiresAt: _expiresAt);
        var replacement = AuthMother.NewRefreshTokenId();

        var result = token.Rotate(replacement, _issuedAt.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().Be(_issuedAt.AddDays(1));
        token.ReplacedByTokenId.Should().Be(replacement);
        token.IsActive(_issuedAt.AddDays(1)).Should().BeFalse("token yang sudah dirotasi tidak lagi aktif");
    }

    [Fact]
    public void Rotating_an_already_revoked_token_is_a_conflict()
    {
        var token = AuthMother.ARefreshToken(issuedAt: _issuedAt, expiresAt: _expiresAt);
        token.Rotate(AuthMother.NewRefreshTokenId(), _issuedAt.AddDays(1));

        var result = token.Rotate(AuthMother.NewRefreshTokenId(), _issuedAt.AddDays(2));

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("refresh_token.already_revoked");
    }

    [Fact]
    public void Revoke_marks_the_token_and_is_idempotent()
    {
        var token = AuthMother.ARefreshToken(issuedAt: _issuedAt, expiresAt: _expiresAt);

        token.Revoke(_issuedAt.AddHours(1));
        token.Revoke(_issuedAt.AddHours(2));

        token.RevokedAt.Should().Be(_issuedAt.AddHours(1), "revoke idempotent — stempel pertama dipertahankan");
        token.IsActive(_issuedAt.AddHours(3)).Should().BeFalse();
    }

    [Fact]
    public void Issue_raises_no_domain_event()
    {
        AuthMother.ARefreshToken().DomainEvents.Should().BeEmpty();
    }
}
