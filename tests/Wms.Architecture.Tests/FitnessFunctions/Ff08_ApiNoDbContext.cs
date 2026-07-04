using NetArchTest.Rules;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#8 — *.Api tak menyentuh DbContext/EF Core. Read didelegasikan ke read-port/MediatR.
public sealed class Ff08_ApiNoDbContext
{
    [Fact]
    public void Api_assemblies_do_not_depend_on_ef_core()
    {
        var apiAssemblies = ArchitectureFixture.ApiAssemblies;
        if (apiAssemblies.IsEmpty)
        {
            return; // vacuous-safe
        }

        var result = Types.InAssemblies(apiAssemblies)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Api menyentuh EF Core (harus lewat read-port): {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
