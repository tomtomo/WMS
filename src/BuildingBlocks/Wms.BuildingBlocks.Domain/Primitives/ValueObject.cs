using System.Diagnostics.CodeAnalysis;

namespace Wms.BuildingBlocks.Domain.Primitives;

// Structural equality — dua value object dengan komponen identik dianggap sama.
[SuppressMessage(
    "Major Code Smell",
    "S3875:\"operator==\" should not be overloaded on reference types",
    Justification = "Value object sengaja memberi semantik nilai lewat == dan !=, bukan reference equality.")]
public abstract class ValueObject
{
    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);

    public override bool Equals(object? obj)
    {
        if (obj is not ValueObject other || other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    protected abstract IEnumerable<object?> GetEqualityComponents();
}
