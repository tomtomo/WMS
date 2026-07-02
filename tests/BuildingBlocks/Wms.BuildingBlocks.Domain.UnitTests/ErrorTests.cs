using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.BuildingBlocks.Domain.UnitTests;

// Test Error: equality struktural
public sealed class ErrorTests
{
    [Fact]
    public void Errors_with_same_code_and_message_are_equal()
    {
        var a = new Error("inventory.insufficient_stock", "Stok tak cukup.");
        var b = new Error("inventory.insufficient_stock", "Stok tak cukup.");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Errors_with_different_code_are_not_equal()
    {
        var a = new Error("inventory.insufficient_stock", "x");
        var b = new Error("inventory.not_found", "x");

        a.Should().NotBe(b);
    }

    [Fact]
    public void None_is_the_empty_sentinel()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Theory]
    [InlineData("inventory.insufficient_stock")]
    [InlineData("id.invalid")]
    [InlineData("result.failure_requires_error")]
    [InlineData("a.b")]
    public void Construction_accepts_well_formed_snake_dot_snake_codes(string code)
    {
        var act = () => new Error(code, "pesan");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Inventory.Foo")]
    [InlineData("nocode")]
    [InlineData("inventory.")]
    [InlineData(".foo")]
    [InlineData("inventory..foo")]
    [InlineData("1nventory.foo")]
    [InlineData("inventory.foo bar")]
    public void Construction_rejects_codes_that_violate_snake_dot_snake(string code)
    {
        var act = () => new Error(code, "pesan");

        act.Should().Throw<ArgumentException>();
    }
}
