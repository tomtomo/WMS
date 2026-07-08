using System.Reflection;
using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Abstractions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Memastikan semua permission yang dipakai aplikasi ada di katalog.
public sealed class PermissionCatalogCoverage
{
    [Fact]
    public void Every_enforced_permission_code_is_in_the_seeded_catalog()
    {
        var enforced = ArchitectureFixture.ModuleAssemblies
            .SelectMany(LoadableTypes)
            .Select(type => type.GetCustomAttribute<RequiresPermissionAttribute>()?.Permission)
            .Where(permission => permission is not null)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        enforced.Should().NotBeEmpty("marker [RequiresPermission] harus ter-deteksi (jaga guard tak vacuous)");

        var catalog = SeededCatalogCodes();
        var missing = enforced
            .Where(code => !catalog.Contains(code!, StringComparer.Ordinal))
            .ToList();

        missing.Should().BeEmpty(
            "tiap kode [RequiresPermission] wajib ada di katalog seed AuthSeeder — cegah drift silent-deny (named FF)");
    }

    private static IReadOnlyCollection<string> SeededCatalogCodes()
    {
        var seeder = ArchitectureFixture.WmsAssemblies
            .Select(assembly => assembly.GetType("Wms.Auth.Infrastructure.Seed.AuthSeeder"))
            .FirstOrDefault(type => type is not null)
            ?? throw new InvalidOperationException("AuthSeeder tidak ditemukan di assembly ter-load.");

        var method = seeder.GetMethod("CatalogCodes", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("AuthSeeder.CatalogCodes() tak ditemukan.");
        return (IReadOnlyCollection<string>)method.Invoke(null, null)!;
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }
}
