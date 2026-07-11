using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// Pastikan Hosts/Azure tidak menggunakan Platform.Local atau adapter lokal seperti RabbitMQ, filesystem, dan Hangfire.
public sealed partial class NoLocalAdapterInCloudHostTests
{
    [Fact]
    public void Cloud_hosts_do_not_reference_platform_local()
    {
        var azureHostsRoot = SourceScan.SrcPath("Hosts", "Azure");

        // Pastikan host Azure tersedia agar pengecekan adapter lokal dijalankan.
        SourceScan.ProjectFiles(azureHostsRoot).Should().NotBeEmpty("host Azure wajib ada di src/Hosts/Azure");

        var violations = new List<string>();
        foreach (var projectFile in SourceScan.ProjectFiles(azureHostsRoot))
        {
            violations.AddRange(
                File.ReadLines(projectFile)
                    .Select(line => ProjectReferenceRegex().Match(line))
                    .Where(match => match.Success && match.Groups[1].Value.Contains(
                        "Wms.Platform.Local", StringComparison.OrdinalIgnoreCase))
                    .Select(match => $"{projectFile}: ProjectReference {match.Groups[1].Value}"));
        }

        foreach (var sourceFile in SourceScan.SourceFiles(azureHostsRoot))
        {
            violations.AddRange(
                File.ReadLines(sourceFile)
                    .Where(line => LocalPlatformUsageRegex().IsMatch(line))
                    .Select(line => $"{sourceFile}: {line.Trim()}"));
        }

        violations.Should().BeEmpty("Hosts/Azure ⊄ Platform.Local — adapter Local dilarang di cloud host");
    }

    [GeneratedRegex(@"<ProjectReference\s+Include=""([^""]+)""")]
    private static partial Regex ProjectReferenceRegex();

    // Deteksi penggunaan namespace Local dan pemanggilan registrasi adapter lokal.
    [GeneratedRegex(@"(^\s*(global\s+)?using\s+(static\s+)?Wms\.Platform\.Local)|(\bAddLocalPlatform\s*\()")]
    private static partial Regex LocalPlatformUsageRegex();
}
