using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Wms.Architecture.Tests;

// What : Smoke test yang membuktikan harness fitness-function (NetArchTest) sudah ter-wire
//        dan bisa load + analisis assembly Wms.*. Ini BUKAN aturan arsitektur sungguhan —
//        rule fitness-function asli menyusul belakangan.
// Why  : harness governance harus terbukti jalan sebelum aturan apa pun ditulis, supaya
//        penambahan rule berikutnya tinggal nempel, tak perlu merakit ulang engine-nya.
// How  : load tiap Wms.*.dll dari output test, lalu jalankan satu predicate NetArchTest
//        end-to-end (lolos karena tak ada yang depend ke System.Web).
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

        // Jalankan engine NetArchTest atas type yang benar-benar ter-load.
        var result = Types.InAssemblies(assemblies)
            .That()
            .ResideInNamespaceStartingWith("Wms")
            .Should()
            .NotHaveDependencyOn("System.Web")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
