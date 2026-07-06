using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Wms.Auth.Infrastructure.Configurations;

// Mapping collection Guid ke jsonb.
internal static class GuidCollectionMapping
{
    // Converter untuk menyimpan collection Guid sebagai JSON.
    public static readonly ValueConverter<IReadOnlyList<Guid>, string> Converter = new(
        value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>());

    // Comparer untuk mendukung change tracking.
    public static readonly ValueComparer<IReadOnlyList<Guid>> Comparer = new(
        (left, right) => (left ?? new List<Guid>()).SequenceEqual(right ?? new List<Guid>()),
        value => value.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
        value => value.ToList());
}
