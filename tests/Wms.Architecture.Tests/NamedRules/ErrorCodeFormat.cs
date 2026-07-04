using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Named FF — Error.Code berformat {snake}.{snake}
public sealed partial class ErrorCodeFormat
{
    [Fact]
    public void Error_code_literals_follow_snake_dot_snake()
    {
        var violations = new List<string>();
        foreach (var sourceFile in SourceScan.SourceFiles(SourceScan.SrcPath()))
        {
            var lineNumber = 0;
            foreach (var line in File.ReadLines(sourceFile))
            {
                lineNumber++;
                foreach (Match match in NewErrorLiteralRegex().Matches(line))
                {
                    var code = match.Groups[1].Value;
                    if (!ErrorCodeRegex().IsMatch(code))
                    {
                        violations.Add($"{sourceFile}:{lineNumber}: Error.Code '{code}'");
                    }
                }
            }
        }

        violations.Should().BeEmpty("Error.Code wajib {snake}.{snake} (named FF, RFC 7807)");
    }

    [GeneratedRegex(@"new\s+Error\s*\(\s*""([^""]*)""")]
    private static partial Regex NewErrorLiteralRegex();

    [GeneratedRegex(@"^[a-z][a-z0-9_]*\.[a-z][a-z0-9_]*$")]
    private static partial Regex ErrorCodeRegex();
}
