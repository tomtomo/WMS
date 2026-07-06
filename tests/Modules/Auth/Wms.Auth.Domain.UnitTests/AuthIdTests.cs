using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Auth.Domain.UnitTests;

// Typed ID Auth: UserId/RoleId/PermissionId/RefreshTokenId (Guid).
public sealed class AuthIdTests
{
    [Fact]
    public void UserId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = UserId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void All_auth_ids_reject_an_empty_guid_as_a_validation_error()
    {
        UserId.Create(Guid.Empty).ErrorType.Should().Be(ResultErrorType.Validation);
        RoleId.Create(Guid.Empty).IsFailure.Should().BeTrue();
        PermissionId.Create(Guid.Empty).IsFailure.Should().BeTrue();
        RefreshTokenId.Create(Guid.Empty).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Ids_of_the_same_type_and_value_are_equal()
    {
        var value = Guid.NewGuid();

        UserId.Create(value).Value.Should().Be(UserId.Create(value).Value);
    }

    [Fact]
    public void Ids_of_different_types_with_the_same_value_are_not_equal()
    {
        var value = Guid.NewGuid();

        var userId = UserId.Create(value).Value;
        var roleId = RoleId.Create(value).Value;

        userId.Equals(roleId).Should().BeFalse();
    }
}
