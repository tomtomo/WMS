using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Wms.Architecture.Tests;

// Smoke test: untuk membuktikan harness NetArchTest bisa load assembly Wms.* dan jalan end to end, belum aturan arsitektur sungguhan.
public sealed class SolutionLoadSmokeTests
{
    [Fact]
    public void Wms_assemblies_load_and_netarchtest_harness_executes()
    {
        var assemblies = Directory
            .EnumerateFiles(AppContext.BaseDirectory, "Wms.*.dll")
            .Select(Assembly.LoadFrom)
            .ToList();

        Assert.NotEmpty(assemblies);

        // Jalankan engine NetArchTest untuk type yang benar-benar terload.
        var result = Types.InAssemblies(assemblies)
            .That()
            .ResideInNamespaceStartingWith("Wms")
            .Should()
            .NotHaveDependencyOn("System.Web")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
