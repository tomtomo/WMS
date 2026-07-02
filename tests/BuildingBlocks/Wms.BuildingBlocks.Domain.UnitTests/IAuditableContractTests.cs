using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Xunit;

namespace Wms.BuildingBlocks.Domain.UnitTests;

// Test IAuditable: tepat empat field audit.
public sealed class IAuditableContractTests
{
    [Fact]
    public void IAuditable_declares_exactly_the_four_audit_members()
    {
        var properties = typeof(IAuditable).GetProperties()
            .Select(p => p.Name)
            .OrderBy(name => name, StringComparer.Ordinal);

        properties.Should().Equal("CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy");
    }

    [Theory]
    [InlineData("CreatedBy", typeof(string))]
    [InlineData("CreatedAt", typeof(DateTimeOffset))]
    [InlineData("ModifiedBy", typeof(string))]
    [InlineData("ModifiedAt", typeof(DateTimeOffset?))]
    public void Each_audit_member_has_the_expected_type_and_is_settable(string name, Type expectedType)
    {
        var property = typeof(IAuditable).GetProperty(name);

        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(expectedType);
        property.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeTrue();
    }

    [Theory]
    [InlineData("Version")]
    [InlineData("RowVersion")]
    [InlineData("DeletedBy")]
    [InlineData("DeletedAt")]
    public void IAuditable_does_not_declare_deferred_or_rejected_members(string forbidden)
    {
        typeof(IAuditable).GetProperty(forbidden).Should().BeNull();
    }
}
