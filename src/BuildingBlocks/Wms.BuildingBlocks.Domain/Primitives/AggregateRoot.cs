using Wms.BuildingBlocks.Domain.Events;

namespace Wms.BuildingBlocks.Domain.Primitives;

// Aggregate root — menjaga konsistensi, menampung domain event in-process. GoF Observer
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    // Raise hanya saat fakta bisnis sukses — tak ada event jika gagal.
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
