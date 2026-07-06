using AwesomeAssertions;
using Wms.Auth.Domain.UnitTests.TestData;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Auth.Domain.UnitTests;

public sealed class RoleTests
{
    [Fact]
    public void Create_starts_an_active_role()
    {
        var result = Role.Create(AuthMother.NewRoleId(), "OPERATOR", "Operator", []);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.Code.Should().Be("OPERATOR");
    }

    [Theory]
    [InlineData("", "Operator", "role.code_required")]
    [InlineData("OPERATOR", "", "role.name_required")]
    public void Create_rejects_missing_details(string code, string name, string expectedCode)
    {
        var result = Role.Create(AuthMother.NewRoleId(), code, name, []);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Create_snapshots_distinct_permission_ids()
    {
        var permissionId = Guid.NewGuid();

        var role = Role.Create(AuthMother.NewRoleId(), "OP", "Operator", [permissionId, permissionId]).Value;

        role.PermissionIds.Should().ContainSingle().Which.Should().Be(permissionId);
    }

    [Fact]
    public void Rename_changes_the_display_name()
    {
        var role = AuthMother.ARole();

        var result = role.Rename("Shift Supervisor");

        result.IsSuccess.Should().BeTrue();
        role.Name.Should().Be("Shift Supervisor");
    }

    [Fact]
    public void Rename_rejects_a_blank_name()
    {
        var result = AuthMother.ARole().Rename("  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("role.name_required");
    }

    [Fact]
    public void Set_permissions_replaces_the_permission_set_without_duplicates()
    {
        var role = AuthMother.ARole();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        role.SetPermissions([first, second, first]);

        role.PermissionIds.Should().BeEquivalentTo([first, second]);
    }

    [Fact]
    public void Deactivate_soft_deletes_and_is_idempotent()
    {
        var role = AuthMother.ARole();

        role.Deactivate();
        var second = role.Deactivate();

        second.IsSuccess.Should().BeTrue();
        role.IsActive.Should().BeFalse();
    }
}
