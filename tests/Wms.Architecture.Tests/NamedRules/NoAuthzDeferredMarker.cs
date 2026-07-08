using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Memastikan tidak ada marker authZ sementara yang tertinggal di source.
public sealed class NoAuthzDeferredMarker
{
    // Hindari self match saat test melakukan scan.
    private static readonly string _marker = string.Concat("AUTHZ", "-DEFERRED");

    [Fact]
    public void No_authz_deferral_marker_remains_in_source()
    {
        var violations = SourceScan.SourceFiles(SourceScan.SrcPath())
            .Where(file => File.ReadAllText(file).Contains(_marker, StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(ArchitectureFixture.RepoRoot, file))
            .ToList();

        violations.Should().BeEmpty(
            "marker deferral authZ wajib bersih setelah milestone Authorization Wire-Up");
    }
}
