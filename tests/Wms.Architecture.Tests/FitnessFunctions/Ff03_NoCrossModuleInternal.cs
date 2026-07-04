using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#3 — modul tak ref internal (Domain/Application/Infrastructure/Api) modul lain. Lintas modul hanya via *.Contracts/*.Grpc.
public sealed class Ff03_NoCrossModuleInternal
{
    private static readonly string[] _internalLayers = ["Domain", "Application", "Infrastructure", "Api"];

    [Fact]
    public void Modules_do_not_reference_other_module_internals()
    {
        var modules = ArchitectureFixture.ModuleAssemblies;

        // Assembly internal per module key pemiliknya — ini terlarang direferensikan modul lain.
        var internalsByModule = modules
            .Where(assembly => _internalLayers.Contains(ArchitectureFixture.LayerOf(assembly)))
            .ToLookup(ArchitectureFixture.ModuleKey, ArchitectureFixture.Name);

        var violations = new List<string>();
        foreach (var module in modules)
        {
            var ownKey = ArchitectureFixture.ModuleKey(module);
            var referenced = ArchitectureFixture.ReferencedWmsAssemblies(module);

            foreach (var otherModule in internalsByModule.Where(group => !string.Equals(group.Key, ownKey, StringComparison.Ordinal)))
            {
                violations.AddRange(otherModule
                    .Where(referenced.Contains)
                    .Select(forbidden => $"{ArchitectureFixture.Name(module)} → {forbidden}"));
            }
        }

        violations.Should().BeEmpty("modul hanya boleh lintas-modul via *.Contracts/*.Grpc (FF#3)");
    }
}
