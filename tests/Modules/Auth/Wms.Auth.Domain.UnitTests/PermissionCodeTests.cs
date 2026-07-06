using AwesomeAssertions;
using Wms.Auth.Domain.ValueObjects;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Auth.Domain.UnitTests;

// PermissionCode wajib berpola Module.Action.
public sealed class PermissionCodeTests
{
    [Theory]
    [InlineData("Inbound.PostGR")]
    [InlineData("Auth.ManageUser")]
    [InlineData("MasterData.ManageProduct")]
    [InlineData("Outbound.DispatchWave")]
    public void Create_accepts_module_dot_action(string code)
    {
        var result = PermissionCode.Create(code);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(code);
    }

    [Theory]
    [InlineData("inbound.PostGR")]
    [InlineData("Inbound")]
    [InlineData("Inbound.")]
    [InlineData(".PostGR")]
    [InlineData("Inbound.Post.GR")]
    [InlineData("Inbound.post")]
    [InlineData("Inbound PostGR")]
    public void Create_rejects_codes_that_break_the_module_action_pattern(string code)
    {
        var result = PermissionCode.Create(code);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission.code_invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_as_required(string code)
    {
        var result = PermissionCode.Create(code);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("permission.code_required");
    }

    [Fact]
    public void Codes_with_the_same_value_are_equal()
    {
        PermissionCode.Create("Inbound.PostGR").Value.Should().Be(PermissionCode.Create("Inbound.PostGR").Value);
    }
}
