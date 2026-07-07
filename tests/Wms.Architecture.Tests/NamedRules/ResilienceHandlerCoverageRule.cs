using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Memastikan semua klien s2s memakai resilience handler yang sesuai.
public sealed class ResilienceHandlerCoverageRule
{
    [Fact]
    public void Klien_grpc_hanya_didaftarkan_lewat_AddInternalGrpcClient()
    {
        var violations = new List<string>();
        foreach (var file in SourceScan.SourceFiles(SourceScan.SrcPath()))
        {
            var text = File.ReadAllText(file);
            var isSanctionedSeam = file.EndsWith(
                Path.Combine("Wms.Platform.Hosting", "InternalGrpcClient.cs"),
                StringComparison.OrdinalIgnoreCase);

            if (!isSanctionedSeam && text.Contains("AddGrpcClient", StringComparison.Ordinal))
            {
                violations.Add($"{Relative(file)}: AddGrpcClient langsung — pakai AddInternalGrpcClient");
            }

            if (text.Contains("GrpcChannel.ForAddress", StringComparison.Ordinal))
            {
                violations.Add($"{Relative(file)}: GrpcChannel telanjang tanpa resilience handler");
            }
        }

        violations.Should().BeEmpty("klien gRPC s2s wajib lewat seam berresilience (named FF)");
    }

    [Fact]
    public void Klien_http_eksplisit_wajib_resilience_handler()
    {
        var violations = new List<string>();
        foreach (var file in SourceScan.SourceFiles(SourceScan.SrcPath()))
        {
            var text = File.ReadAllText(file);

            // Abaikan konfigurasi HttpClient yang bukan registrasi klien.
            if (!text.Contains("AddHttpClient(", StringComparison.Ordinal)
                && !text.Contains("AddHttpClient<", StringComparison.Ordinal))
            {
                continue;
            }

            if (!text.Contains("AddHttpResilience", StringComparison.Ordinal)
                && !text.Contains("AddStandardResilienceHandler", StringComparison.Ordinal)
                && !text.Contains("AddGrpcResilience", StringComparison.Ordinal))
            {
                violations.Add(Relative(file));
            }
        }

        violations.Should().BeEmpty("registrasi HttpClient wajib attach resilience handler (named FF)");
    }

    [Fact]
    public void Default_http_global_memasang_resilience()
    {
        var hostingResilience = SourceScan.SrcPath("Platform", "Wms.Platform.Hosting", "HostingResilience.cs");
        File.Exists(hostingResilience).Should().BeTrue("HostingResilience.cs adalah default global HttpClient");

        var text = File.ReadAllText(hostingResilience);
        text.Should().Contain("ConfigureHttpClientDefaults");
        text.Should().Contain("AddHttpResilience", because: "default global tak boleh kehilangan standard handler");
    }

    [Fact]
    public void Jalur_dlq_outbox_eventing_bebas_polly()
    {
        var roots = new[]
        {
            SourceScan.SrcPath("BuildingBlocks", "Wms.BuildingBlocks.Infrastructure", "DeadLetter"),
            SourceScan.SrcPath("BuildingBlocks", "Wms.BuildingBlocks.Infrastructure", "Outbox"),
            SourceScan.SrcPath("BuildingBlocks", "Wms.BuildingBlocks.Infrastructure", "Eventing"),
        };

        var violations = new List<string>();
        foreach (var file in roots.SelectMany(SourceScan.SourceFiles))
        {
            if (File.ReadAllText(file).Contains("Polly", StringComparison.Ordinal))
            {
                violations.Add(Relative(file));
            }
        }

        violations.Should().BeEmpty("retry DLQ = manual loop, bukan Polly");
    }

    private static string Relative(string file) => Path.GetRelativePath(ArchitectureFixture.RepoRoot, file);
}
