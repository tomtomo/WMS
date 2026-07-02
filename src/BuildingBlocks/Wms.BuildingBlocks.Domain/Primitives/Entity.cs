using System.Diagnostics.CodeAnalysis;

namespace Wms.BuildingBlocks.Domain.Primitives;

// Identity equality — entity sama bila Id sama.
[SuppressMessage(
    "Major Code Smell",
    "S3875:\"operator==\" should not be overloaded on reference types",
    Justification = "Entity sengaja memberi semantik identity lewat == dan !=, dibanding by Id.")]
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other || other.GetType() != GetType())
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Transient (Id default) hanya equal ke dirinya sendiri, tak pernah ke transient lain.
        if (IsTransient() || other.IsTransient())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    private bool IsTransient() => EqualityComparer<TId>.Default.Equals(Id, default!);
}
