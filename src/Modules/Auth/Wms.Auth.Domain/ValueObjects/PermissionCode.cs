using System.Text.RegularExpressions;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain.ValueObjects;

// Kode permission dengan format Module.Action (mis. Inbound.PostGR).
public sealed partial class PermissionCode : ValueObject
{
    private PermissionCode(string value) => Value = value;

    public string Value { get; }

    public static Result<PermissionCode> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<PermissionCode>(new Error("permission.code_required", "Permission code wajib diisi."));
        }

        var trimmed = value.Trim();
        if (!PatternRegex().IsMatch(trimmed))
        {
            return Result.Invalid<PermissionCode>(
                new Error("permission.code_invalid", "Permission code harus berpola Module.Action (mis. Inbound.PostGR)."));
        }

        return Result.Success(new PermissionCode(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[A-Z][A-Za-z]+\.[A-Z][A-Za-z]+$")]
    private static partial Regex PatternRegex();
}
