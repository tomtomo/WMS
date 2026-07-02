using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Domain.UnitTests;

// Test ValueObject: equality struktural dan konsistensi GetHashCode.
public sealed class ValueObjectTests
{
    [Fact]
    public void Value_objects_with_identical_components_are_equal()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Value_objects_with_a_differing_component_are_not_equal()
    {
        var a = new Money(10m, "USD");

        a.Should().NotBe(new Money(10m, "EUR"));
        a.Should().NotBe(new Money(20m, "USD"));
        (a != new Money(20m, "USD")).Should().BeTrue();
    }

    [Fact]
    public void Equal_value_objects_share_the_same_hash_code()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "USD");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Value_objects_of_different_types_are_never_equal()
    {
        var money = new Money(10m, "USD");
        var weight = new Weight(10m, "USD");

        money.Equals(weight).Should().BeFalse();
    }
}
