using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Domain.UnitTests;

// Test Entity: identity equality by Id dan penanganan transient.
public sealed class EntityTests
{
    [Fact]
    public void Entities_with_the_same_id_are_equal()
    {
        var id = SampleId.Create(Guid.NewGuid()).Value;

        var a = new SampleEntity(id);
        var b = new SampleEntity(id);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var a = new SampleEntity(SampleId.Create(Guid.NewGuid()).Value);
        var b = new SampleEntity(SampleId.Create(Guid.NewGuid()).Value);

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void An_entity_is_equal_to_itself()
    {
        var entity = new SampleEntity(SampleId.Create(Guid.NewGuid()).Value);

        entity.Equals(entity).Should().BeTrue();
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        var id = SampleId.Create(Guid.NewGuid()).Value;

        var entity = new SampleEntity(id);
        var another = new AnotherEntity(id);

        entity.Equals(another).Should().BeFalse();
    }

    [Fact]
    public void Two_transient_entities_with_default_ids_are_not_equal()
    {
        var a = new ValueKeyedEntity(0);
        var b = new ValueKeyedEntity(0);

        a.Should().NotBe(b);
    }

    [Fact]
    public void A_transient_entity_is_equal_to_itself()
    {
        var entity = new ValueKeyedEntity(0);

        entity.Equals(entity).Should().BeTrue();
    }

    [Fact]
    public void Persisted_entities_with_the_same_value_id_are_equal()
    {
        new ValueKeyedEntity(7).Should().Be(new ValueKeyedEntity(7));
    }
}
