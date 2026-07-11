using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// SDK cloud (Azure/Microsoft.Azure/Google.Cloud/Amazon/AWSSDK) hanya boleh ada di Platform.<Cloud>+Hosts.
// Kernel, modul, Platform agnostic tidak boleh ada SDK cloud. Diverifikasi di source.
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

            // Adapter shared digunakan lintas platform, jadi tidak boleh bergantung pada SDK cloud tertentu.
            SourceScan.SrcPath("Platform", "Wms.Platform.Shared"),
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

        violations.Should().BeEmpty("SDK cloud hanya boleh di Platform.<Cloud>+Hosts");
    }

    [Fact]
    public void Azure_sdk_is_confined_to_platform_azure_and_hosts()
    {
        var platformAzureRoot = SourceScan.SrcPath("Platform", "Wms.Platform.Azure");
        string[] allowedRoots = [platformAzureRoot, SourceScan.SrcPath("Hosts")];

        // Pastikan root yang dikecualikan ada, agar test tidak lolos hanya karena path salah.
        SourceScan.ProjectFiles(platformAzureRoot).Should().NotBeEmpty("Wms.Platform.Azure wajib ada");

        var violations = SourceScan.SourceFiles(SourceScan.SrcPath())
            .Where(file => !allowedRoots.Any(root => file.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(file => File.ReadLines(file)
                .Where(line => AzureUsingRegex().IsMatch(line))
                .Select(line => $"{file}: {line.Trim()}"))
            .ToList();

        violations.Should().BeEmpty("Azure.* hanya boleh ada di Platform.Azure dan Hosts ");
    }

    [GeneratedRegex(@"<PackageReference\s+Include=""([^""]+)""")]
    private static partial Regex PackageReferenceRegex();

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?(Azure|Microsoft\.Azure)[\.;]")]
    private static partial Regex AzureUsingRegex();

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?(Azure|Microsoft\.Azure|Google\.Cloud|Amazon)[\.;]")]
    private static partial Regex CloudUsingRegex();
}
