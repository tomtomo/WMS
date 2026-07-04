using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#6 — Platform.* tak ref Modules.* (adapter cuma implement port abstrak di BuildingBlocks.Application).
// Diperiksa via assembly-reference.
public sealed class Ff06_PlatformNoModules
{
    [Fact]
    public void Platform_does_not_reference_modules()
    {
        var moduleNames = ArchitectureFixture.ModuleAssemblies
            .Select(ArchitectureFixture.Name)
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();
        foreach (var platform in ArchitectureFixture.PlatformAssemblies)
        {
            violations.AddRange(ArchitectureFixture.ReferencedWmsAssemblies(platform)
                .Where(moduleNames.Contains)
                .Select(reference => $"{ArchitectureFixture.Name(platform)} → {reference}"));
        }

        violations.Should().BeEmpty("Platform hanya implement port abstrak, tak kenal Modules (FF#6)");
    }
}
