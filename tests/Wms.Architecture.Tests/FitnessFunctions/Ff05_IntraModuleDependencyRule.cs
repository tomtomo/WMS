using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#5 — intra modul: Domain tak ref Application/Infrastructure/Api. Application tak ref Infrastructure/Api.
// Arah dependensi menuju Domain. Diperiksa via assembly-reference dalam satu module key.
public sealed class Ff05_IntraModuleDependencyRule
{
    // layer sesama-modul yang tak boleh direferensikannya (pelanggaran arah menuju Domain).
    private static readonly Dictionary<string, string[]> _forbiddenOutwardReferences = new(StringComparer.Ordinal)
    {
        ["Domain"] = ["Application", "Infrastructure", "Api"],
        ["Application"] = ["Infrastructure", "Api"],
    };

    [Fact]
    public void Module_layers_respect_dependency_rule()
    {
        var violations = new List<string>();
        foreach (var module in ArchitectureFixture.ModuleAssemblies.ToLookup(ArchitectureFixture.ModuleKey))
        {
            var layerToAssemblyName = module.ToDictionary(ArchitectureFixture.LayerOf, ArchitectureFixture.Name, StringComparer.Ordinal);

            foreach (var layerAssembly in module)
            {
                if (!_forbiddenOutwardReferences.TryGetValue(ArchitectureFixture.LayerOf(layerAssembly), out var forbiddenLayers))
                {
                    continue;
                }

                var referenced = ArchitectureFixture.ReferencedWmsAssemblies(layerAssembly);
                violations.AddRange(forbiddenLayers
                    .Where(forbiddenLayer => layerToAssemblyName.TryGetValue(forbiddenLayer, out var name) && referenced.Contains(name))
                    .Select(forbiddenLayer => $"{ArchitectureFixture.Name(layerAssembly)} → {layerToAssemblyName[forbiddenLayer]}"));
            }
        }

        violations.Should().BeEmpty("arah dependensi intra-modul harus menuju Domain (FF#5)");
    }
}
