using AwesomeAssertions;
using Wms.Auth.Domain.UnitTests.TestData;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Auth.Domain.UnitTests;

public sealed class UserTests
{
    private static readonly DateTimeOffset _t0 = new(2026, 7, 6, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_an_active_user_with_zero_failed_logins()
    {
        var result = User.Create(AuthMother.NewUserId(), "operator1", "op1@wms.local", "hash", [], []);

        result.IsSuccess.Should().BeTrue();
        var user = result.Value;
        user.Status.Should().Be(UserStatus.Active);
        user.IsActive.Should().BeTrue();
        user.FailedLoginCount.Should().Be(0);
        user.LockedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("", "op@wms.local", "hash", "user.username_required")]
    [InlineData("op", "", "hash", "user.email_required")]
    [InlineData("op", "op@wms.local", "", "user.password_hash_required")]
    public void Create_rejects_missing_details(string username, string email, string passwordHash, string expectedCode)
    {
        var result = User.Create(AuthMother.NewUserId(), username, email, passwordHash, [], []);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Create_snapshots_distinct_role_ids()
    {
        var roleId = Guid.NewGuid();

        var user = User.Create(AuthMother.NewUserId(), "op", "op@wms.local", "hash", [roleId, roleId], []).Value;

        user.RoleIds.Should().ContainSingle().Which.Should().Be(roleId);
    }

    [Fact]
    public void Create_drops_empty_warehouse_ids()
    {
        var warehouseId = Guid.NewGuid();

        var user = User.Create(AuthMother.NewUserId(), "op", "op@wms.local", "hash", [], [Guid.Empty, warehouseId, warehouseId]).Value;

        user.AssignedWarehouseIds.Should().ContainSingle().Which.Should().Be(warehouseId);
    }

    [Fact]
    public void A_user_follows_the_auditable_convention()
    {
        AuthMother.AUser().Should().BeAssignableTo<IAuditable>();
    }

    [Fact]
    public void Create_raises_no_domain_event_because_auth_is_read_only_to_core()
    {
        AuthMother.AUser().DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Failed_logins_below_threshold_keep_the_user_active()
    {
        var user = AuthMother.AUser();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            user.RecordFailedLogin(_t0);
        }

        user.Status.Should().Be(UserStatus.Active);
        user.FailedLoginCount.Should().Be(4);
    }

    [Fact]
    public void Failed_logins_reaching_the_threshold_lock_the_user()
    {
        var user = AuthMother.AUser();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RecordFailedLogin(_t0);
        }

        user.Status.Should().Be(UserStatus.Locked);
        user.LockedAt.Should().Be(_t0);
    }

    [Fact]
    public void Recording_a_failed_login_on_a_disabled_user_is_invalid()
    {
        var user = AuthMother.AUser();
        user.Disable();

        var result = user.RecordFailedLogin(_t0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.disabled");
    }

    [Fact]
    public void A_successful_login_resets_the_failed_counter()
    {
        var user = AuthMother.AUser();
        user.RecordFailedLogin(_t0);
        user.RecordFailedLogin(_t0);

        var result = user.RecordSuccessfulLogin();

        result.IsSuccess.Should().BeTrue();
        user.FailedLoginCount.Should().Be(0);
    }

    [Fact]
    public void A_locked_user_cannot_record_a_successful_login()
    {
        var user = LockedUser();

        var result = user.RecordSuccessfulLogin();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("user.locked");
    }

    [Fact]
    public void A_disabled_user_rejection_is_distinct_from_a_locked_one()
    {
        var disabled = AuthMother.AUser();
        disabled.Disable();

        var disabledRejection = disabled.RecordSuccessfulLogin();
        var lockedRejection = LockedUser().RecordSuccessfulLogin();

        disabledRejection.Error.Code.Should().Be("user.disabled");
        lockedRejection.Error.Code.Should().Be("user.locked");
        disabledRejection.Error.Code.Should().NotBe(lockedRejection.Error.Code);
    }

    [Fact]
    public void Lockout_is_not_expired_before_the_cooldown_elapses()
    {
        var user = LockedUser();

        user.IsLockoutExpired(_t0 + TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void Lockout_is_expired_once_the_cooldown_elapses()
    {
        var user = LockedUser();

        user.IsLockoutExpired(_t0 + TimeSpan.FromMinutes(15)).Should().BeTrue();
    }

    [Fact]
    public void Unlocking_a_locked_user_returns_it_to_active_and_resets_the_counter()
    {
        var user = LockedUser();

        var result = user.Unlock();

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        user.FailedLoginCount.Should().Be(0);
        user.LockedAt.Should().BeNull();
    }

    [Fact]
    public void A_disabled_user_cannot_be_unlocked()
    {
        var user = AuthMother.AUser();
        user.Disable();

        var result = user.Unlock();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.disabled");
    }

    [Fact]
    public void Disabling_soft_deletes_the_user()
    {
        var user = AuthMother.AUser();

        user.Disable();

        user.Status.Should().Be(UserStatus.Disabled);
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Enabling_a_disabled_user_returns_it_to_a_clean_active_state()
    {
        var user = AuthMother.AUser();
        user.Disable();

        user.Enable();

        user.Status.Should().Be(UserStatus.Active);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Assigning_a_role_is_idempotent()
    {
        var user = AuthMother.AUser();
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId);
        user.AssignRole(roleId);

        user.RoleIds.Should().ContainSingle().Which.Should().Be(roleId);
    }

    [Fact]
    public void Assigning_an_empty_warehouse_id_is_invalid()
    {
        var result = AuthMother.AUser().AssignWarehouse(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.warehouse_invalid");
    }

    private static User LockedUser()
    {
        var user = AuthMother.AUser();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            user.RecordFailedLogin(_t0);
        }

        return user;
    }
}
