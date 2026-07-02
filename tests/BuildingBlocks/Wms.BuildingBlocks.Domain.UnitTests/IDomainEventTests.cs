using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Events;
using Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Domain.UnitTests;

// Memastikan IDomainEvent jadi marker yang bisa diimplement event record immutable.
public sealed class IDomainEventTests
{
    [Fact]
    public void A_domain_event_record_implements_the_marker()
    {
        var raised = new SampleRaised(Guid.NewGuid(), 1);

        raised.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void Domain_events_with_the_same_data_are_value_equal()
    {
        var id = Guid.NewGuid();

        var first = new SampleRaised(id, 1);
        var second = new SampleRaised(id, 1);

        first.Should().Be(second);
    }
}
