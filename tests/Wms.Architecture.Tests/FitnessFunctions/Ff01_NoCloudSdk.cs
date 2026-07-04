using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#1 — SDK cloud (Azure/Microsoft.Azure/Google.Cloud/Amazon/AWSSDK) hanya boleh hidup di Platform.<Cloud>+Hosts.
// Kernel + modul + Platform agnostic (Hosting/Local) nol SDK cloud. Diverifikasi di source.
public sealed partial class Ff01_NoCloudSdk
{
    private static readonly string[] _cloudSdkPrefixes = ["Azure.", "Microsoft.Azure", "Google.Cloud", "Amazon", "AWSSDK"];

    [Fact]
    public void No_cloud_sdk_in_building_blocks_modules_and_agnostic_platform()
    {
        string[] scanRoots =
        [
            SourceScan.SrcPath("BuildingBlocks"),
            SourceScan.SrcPath("Modules"),
            SourceScan.SrcPath("Platform", "Wms.Platform.Hosting"),
            SourceScan.SrcPath("Platform", "Wms.Platform.Local"),
        ];

        var violations = new List<string>();
        foreach (var scanRoot in scanRoots)
        {
            foreach (var projectFile in SourceScan.ProjectFiles(scanRoot))
            {
                violations.AddRange(
                    File.ReadLines(projectFile)
                        .Select(line => PackageReferenceRegex().Match(line))
                        .Where(match => match.Success && _cloudSdkPrefixes.Any(
                            prefix => match.Groups[1].Value.StartsWith(prefix, StringComparison.Ordinal)))
                        .Select(match => $"{projectFile}: PackageReference {match.Groups[1].Value}"));
            }

            foreach (var sourceFile in SourceScan.SourceFiles(scanRoot))
            {
                violations.AddRange(
                    File.ReadLines(sourceFile)
                        .Where(line => CloudUsingRegex().IsMatch(line))
                        .Select(line => $"{sourceFile}: {line.Trim()}"));
            }
        }

        violations.Should().BeEmpty("SDK cloud hanya boleh di Platform.<Cloud>+Hosts (FF#1)");
    }

    [GeneratedRegex(@"<PackageReference\s+Include=""([^""]+)""")]
    private static partial Regex PackageReferenceRegex();

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?(Azure|Microsoft\.Azure|Google\.Cloud|Amazon)[\.;]")]
    private static partial Regex CloudUsingRegex();
}
