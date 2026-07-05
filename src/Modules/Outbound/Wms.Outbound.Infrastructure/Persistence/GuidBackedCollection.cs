using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Wms.Outbound.Infrastructure.Persistence;

// Typed id disimpan sebagai Guid mentah dan dibangun ulang lewat factory.
internal static class GuidBackedCollection
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static ValueConverter<List<T>, string> Converter<T>(Func<T, Guid> toGuid, Func<Guid, T> fromGuid) =>
        new(
            ids => JsonSerializer.Serialize(ids.Select(toGuid).ToList(), _options),
            json => (JsonSerializer.Deserialize<List<Guid>>(json, _options) ?? new List<Guid>()).Select(fromGuid).ToList());

    public static ValueComparer<List<T>> Comparer<T>() =>
        new(
            (left, right) => (left ?? new List<T>()).SequenceEqual(right ?? new List<T>()),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item!.GetHashCode())),
            list => list.ToList());
}
