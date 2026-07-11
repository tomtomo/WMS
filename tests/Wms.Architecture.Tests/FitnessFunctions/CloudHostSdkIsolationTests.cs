using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// Pastikan SDK Azure hanya digunakan di Hosts/Azure.
// Hosts/Local dan Hosts/Gcp tidak boleh memiliki dependency ke Azure.
public sealed partial class CloudHostSdkIsolationTests
{
    private static readonly string[] _azureSdkPrefixes = ["Azure.", "Microsoft.Azure"];

    [Fact]
    public void Azure_sdk_in_hosts_is_confined_to_hosts_azure()
    {
        var hostsRoot = SourceScan.SrcPath("Hosts");
        var azureHostsRoot = SourceScan.SrcPath("Hosts", "Azure");

        // Pastikan host Azure tersedia agar aturan isolasi SDK tetap relevan.
        SourceScan.ProjectFiles(azureHostsRoot).Should().NotBeEmpty("host Azure wajib ada di src/Hosts/Azure");

        var violations = new List<string>();
        foreach (var projectFile in SourceScan.ProjectFiles(hostsRoot))
        {
            if (projectFile.StartsWith(azureHostsRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            violations.AddRange(
                File.ReadLines(projectFile)
                    .Select(line => PackageReferenceRegex().Match(line))
                    .Where(match => match.Success && _azureSdkPrefixes.Any(
                        prefix => match.Groups[1].Value.StartsWith(prefix, StringComparison.Ordinal)))
                    .Select(match => $"{projectFile}: PackageReference {match.Groups[1].Value}"));
        }

        foreach (var sourceFile in SourceScan.SourceFiles(hostsRoot))
        {
            if (sourceFile.StartsWith(azureHostsRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            violations.AddRange(
                File.ReadLines(sourceFile)
                    .Where(line => AzureUsingRegex().IsMatch(line))
                    .Select(line => $"{sourceFile}: {line.Trim()}"));
        }

        violations.Should().BeEmpty("SDK Azure di src/Hosts hanya boleh di Hosts/Azure");
    }

    [Fact]
    public void Hosts_azure_never_references_other_cloud_sdk()
    {
        var azureHostsRoot = SourceScan.SrcPath("Hosts", "Azure");
        SourceScan.ProjectFiles(azureHostsRoot).Should().NotBeEmpty("host Azure wajib ada di src/Hosts/Azure");

        var violations = new List<string>();
        foreach (var projectFile in SourceScan.ProjectFiles(azureHostsRoot))
        {
            violations.AddRange(
                File.ReadLines(projectFile)
                    .Select(line => PackageReferenceRegex().Match(line))
                    .Where(match => match.Success && OtherCloudPrefixes().IsMatch(match.Groups[1].Value))
                    .Select(match => $"{projectFile}: PackageReference {match.Groups[1].Value}"));
        }

        foreach (var sourceFile in SourceScan.SourceFiles(azureHostsRoot))
        {
            violations.AddRange(
                File.ReadLines(sourceFile)
                    .Where(line => OtherCloudUsingRegex().IsMatch(line))
                    .Select(line => $"{sourceFile}: {line.Trim()}"));
        }

        violations.Should().BeEmpty("Hosts/Azure tidak boleh menyeret SDK cloud lain (Google.Cloud/Amazon)");
    }

    [GeneratedRegex(@"<PackageReference\s+Include=""([^""]+)""")]
    private static partial Regex PackageReferenceRegex();

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?(Azure|Microsoft\.Azure)[\.;]")]
    private static partial Regex AzureUsingRegex();

    [GeneratedRegex(@"^(Google\.Cloud|Amazon|AWSSDK)")]
    private static partial Regex OtherCloudPrefixes();

    [GeneratedRegex(@"^\s*(global\s+)?using\s+(static\s+)?(Google\.Cloud|Amazon)[\.;]")]
    private static partial Regex OtherCloudUsingRegex();
}
