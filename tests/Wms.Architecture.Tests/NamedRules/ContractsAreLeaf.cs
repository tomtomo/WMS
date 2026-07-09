using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Memastikan semua *.Contracts tetap ringan. Kontrak hanya boleh bergantung ke Wms.Contracts.Abstractions, atau tidak punya dependency sama sekali.
public sealed class ContractsAreLeaf
{
    private const string LeafAssembly = "Wms.Contracts.Abstractions";

    // Framework pipeline yang tidak boleh ikut terbawa oleh project Contracts.
    private static readonly string[] _forbiddenFramework =
        ["MediatR", "FluentValidation", "Microsoft.FeatureManagement"];

    [Fact]
    public void Contracts_reference_only_the_leaf_or_nothing()
    {
        var violations = new List<string>();
        foreach (var contracts in ArchitectureFixture.ContractsAssemblies)
        {
            violations.AddRange(ArchitectureFixture.ReferencedWmsAssemblies(contracts)
                .Where(reference => !string.Equals(reference, LeafAssembly, StringComparison.Ordinal))
                .Select(reference => $"{ArchitectureFixture.Name(contracts)} → {reference}"));
        }

        violations.Should().BeEmpty(
            $"*.Contracts hanya boleh mereferensikan {LeafAssembly}, atau tidak mereferensikan project Wms lain");
    }

    [Fact]
    public void Contracts_leaf_drags_no_kernel_or_framework()
    {
        var leaf = ArchitectureFixture.WmsAssemblies.FirstOrDefault(
            assembly => string.Equals(ArchitectureFixture.Name(assembly), LeafAssembly, StringComparison.Ordinal));

        leaf.Should().NotBeNull($"{LeafAssembly} harus ada sebagai project kontrak dasar");

        // Project kontrak dasar harus tetap berdiri sendiri tanpa referensi ke project Wms lain.
        ArchitectureFixture.ReferencedWmsAssemblies(leaf!)
            .Should().BeEmpty($"{LeafAssembly} wajib daun murni tanpa ref Wms.* (EV-24)");

        // Pastikan project kontrak dasar tidak membawa dependency pipeline aplikasi.
        var leaked = leaf!.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => _forbiddenFramework.Any(framework => name.StartsWith(framework, StringComparison.Ordinal)))
            .ToList();

        leaked.Should().BeEmpty($"{LeafAssembly} tidak boleh membawa MediatR, FluentValidation, atau FeatureManagement");
    }
}
