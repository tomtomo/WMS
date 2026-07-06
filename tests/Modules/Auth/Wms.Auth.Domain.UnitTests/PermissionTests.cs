using AwesomeAssertions;
using Wms.Auth.Domain.UnitTests.TestData;
using Wms.Auth.Domain.ValueObjects;
using Xunit;

namespace Wms.Auth.Domain.UnitTests;

public sealed class PermissionTests
{
    [Fact]
    public void Create_succeeds_with_a_valid_code_and_description()
    {
        var result = Permission.Create(AuthMother.NewPermissionId(), PermissionCode.Create("Auth.ManageUser").Value, "Kelola user");

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Value.Should().Be("Auth.ManageUser");
        result.Value.Description.Should().Be("Kelola user");
    }

    [Fact]
    public void Create_rejects_a_blank_description()
    {
        var result = Permission.Create(AuthMother.NewPermissionId(), PermissionCode.Create("Auth.ManageUser").Value, "  ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission.description_required");
    }
}
