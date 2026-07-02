using System.Text.RegularExpressions;

namespace Wms.BuildingBlocks.Domain.Results;

// Error sebagai value.
public sealed partial record Error
{
    // Null Object — untuk jalur Result sukses.
    public static readonly Error None = new();

    public Error(string code, string message)
    {
        if (!CodeRegex().IsMatch(code))
        {
            throw new ArgumentException(
                $"Error.Code '{code}' tidak sesuai format {{snake}}.{{snake}}.",
                nameof(code));
        }

        Code = code;
        Message = message;
    }

    private Error()
    {
        Code = string.Empty;
        Message = string.Empty;
    }

    public string Code { get; }

    public string Message { get; }

    [GeneratedRegex(@"^[a-z][a-z0-9_]*\.[a-z][a-z0-9_]*$")]
    private static partial Regex CodeRegex();
}
