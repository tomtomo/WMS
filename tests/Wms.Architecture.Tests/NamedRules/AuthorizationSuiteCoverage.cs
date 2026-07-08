using AwesomeAssertions;
using Xunit;

namespace Wms.Architecture.Tests.NamedRules;

// Memastikan seluruh pengujian authorization tetap tersedia.
public sealed class AuthorizationSuiteCoverage
{
    [Fact]
    public void Warehouse_scoping_suite_is_present() =>
        AssertExists("tests", "Integration", "Wms.Authorization.IntegrationTests", "WarehouseScopingTests.cs");

    [Fact]
    public void Permission_enforcement_and_isactive_suite_is_present() =>
        AssertExists("tests", "Modules", "Auth", "Wms.Auth.IntegrationTests", "AuthorizationEnforcementTests.cs");

    private static void AssertExists(params string[] segments)
    {
        var path = Path.Combine([ArchitectureFixture.RepoRoot, .. segments]);
        File.Exists(path).Should().BeTrue($"authZ-suite gate: {Path.Combine(segments)} wajib ada (jangan hapus tanpa pengganti)");
    }
}
