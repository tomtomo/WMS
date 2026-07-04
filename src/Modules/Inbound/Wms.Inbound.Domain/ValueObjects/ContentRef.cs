using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inbound.Domain.ValueObjects;

// Referensi konten biner di object store — domain hanya menyimpan ref, bukan byte.
public sealed record ContentRef
{
    private ContentRef(string value) => Value = value;

    public string Value { get; }

    public static Result<ContentRef> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<ContentRef>(new Error("content_ref.value_required", "ContentRef wajib diisi."));
        }

        return Result.Success(new ContentRef(value.Trim()));
    }
}
