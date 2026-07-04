using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#7 — nol business throw di *.Domain. Error bisnis mengalir lewat Result.
public sealed partial class Ff07_DomainNoBusinessThrow
{
    private static readonly string[] _whitelistedGuards =
    [
        "ArgumentException",
        "ArgumentNullException",
        "ArgumentOutOfRangeException",
        "InvalidOperationException",
        "NotSupportedException",
        "UnreachableException",
    ];

    [Fact]
    public void Domain_source_has_no_business_throw()
    {
        var violations = new List<string>();
        foreach (var sourceFile in DomainSourceRoots().SelectMany(SourceScan.SourceFiles))
        {
            var lineNumber = 0;
            foreach (var line in File.ReadLines(sourceFile))
            {
                lineNumber++;
                var match = ThrowNewRegex().Match(line);
                if (match.Success && !_whitelistedGuards.Contains(match.Groups[1].Value))
                {
                    violations.Add($"{sourceFile}:{lineNumber}: throw new {match.Groups[1].Value}");
                }
            }
        }

        violations.Should().BeEmpty("Domain harus mengalirkan error bisnis lewat Result, bukan throw (FF#7)");
    }

    // Root source tiap assembly Domain: kernel BuildingBlocks.Domain + tiap folder *.Domain di modul.
    private static IEnumerable<string> DomainSourceRoots()
    {
        yield return SourceScan.SrcPath("BuildingBlocks", "Wms.BuildingBlocks.Domain");

        var modulesRoot = SourceScan.SrcPath("Modules");
        if (Directory.Exists(modulesRoot))
        {
            foreach (var domainDir in Directory.EnumerateDirectories(modulesRoot, "*.Domain", SearchOption.AllDirectories))
            {
                yield return domainDir;
            }
        }
    }

    [GeneratedRegex(@"throw\s+new\s+([A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex ThrowNewRegex();
}
