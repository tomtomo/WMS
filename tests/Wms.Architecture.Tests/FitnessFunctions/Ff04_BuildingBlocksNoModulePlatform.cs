using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#4 — BuildingBlocks tak ref Modules/Platform (kernel tak kenal konsumennya). Diperiksa via assembly-reference.
public sealed class Ff04_BuildingBlocksNoModulePlatform
{
    [Fact]
    public void Building_blocks_do_not_reference_modules_or_platform()
    {
        var forbidden = ArchitectureFixture.PlatformAssemblies
            .Concat(ArchitectureFixture.ModuleAssemblies)
            .Select(ArchitectureFixture.Name)
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();
        foreach (var buildingBlock in ArchitectureFixture.BuildingBlocksAssemblies)
        {
            violations.AddRange(ArchitectureFixture.ReferencedWmsAssemblies(buildingBlock)
                .Where(forbidden.Contains)
                .Select(reference => $"{ArchitectureFixture.Name(buildingBlock)} → {reference}"));
        }

        violations.Should().BeEmpty("kernel BuildingBlocks harus agnostic terhadap Modules/Platform (FF#4)");
    }
}
