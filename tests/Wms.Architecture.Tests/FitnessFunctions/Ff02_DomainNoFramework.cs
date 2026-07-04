using NetArchTest.Rules;
using Xunit;

namespace Wms.Architecture.Tests.FitnessFunctions;

// FF#2 — *.Domain (kernel BuildingBlocks.Domain + Domain modul) nol framework: EF Core / MediatR / ASP.NET.
// Aturan bisnis tak bergantung mekanisme. → Dependency Rule (Martin, Clean Architecture).
public sealed class Ff02_DomainNoFramework
{
    private static readonly string[] _forbiddenFrameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "MediatR",
        "Microsoft.AspNetCore",
    ];

    [Fact]
    public void Domain_assemblies_do_not_depend_on_frameworks()
    {
        var domainAssemblies = ArchitectureFixture.DomainAssemblies;
        if (domainAssemblies.IsEmpty)
        {
            return;
        }

        var result = Types.InAssemblies(domainAssemblies)
            .Should()
            .NotHaveDependencyOnAny(_forbiddenFrameworks)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Domain menyentuh framework terlarang: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
