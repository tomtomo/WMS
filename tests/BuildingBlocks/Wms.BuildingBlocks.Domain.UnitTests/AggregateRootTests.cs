using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Events;
using Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Domain.UnitTests;

// Test AggregateRoot: raise, pull read-only, clear domain event.
public sealed class AggregateRootTests
{
    [Fact]
    public void A_new_aggregate_has_no_domain_events()
    {
        NewAggregate().DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raising_events_appends_them_in_order()
    {
        var aggregate = NewAggregate();

        aggregate.DoSomething(1);
        aggregate.DoSomething(2);
        aggregate.DoSomething(3);

        aggregate.DomainEvents.Should().HaveCount(3);
        aggregate.DomainEvents.OfType<SampleRaised>().Select(e => e.Sequence)
            .Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void Clearing_removes_all_domain_events()
    {
        var aggregate = NewAggregate();
        aggregate.DoSomething(1);
        aggregate.DoSomething(2);

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Published_domain_events_cannot_be_mutated_by_callers()
    {
        var aggregate = NewAggregate();
        aggregate.DoSomething(1);

        var act = () =>
            ((ICollection<IDomainEvent>)aggregate.DomainEvents).Add(new SampleRaised(Guid.NewGuid(), 99));

        act.Should().Throw<NotSupportedException>();
    }

    private static SampleAggregate NewAggregate() => new(SampleId.Create(Guid.NewGuid()).Value);
}
