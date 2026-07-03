using System.Linq;
using NetArchTest.Rules;
using Wms.BuildingBlocks.Application;
using Wms.BuildingBlocks.Infrastructure;
using Xunit;

namespace Wms.Architecture.Tests;

// Test Fitness function
public sealed class FitnessFunctionTests
{
    // BuildingBlocks tak boleh depend ke Modules atau Platform — menjaga arah dependency inti tetap ke dalam.
    [Fact]
    public void Ff04_application_building_block_does_not_depend_on_modules_or_platform()
    {
        var result = Types
            .InAssembly(typeof(IApplicationBuildingBlocksMarker).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Wms.Modules", "Wms.Platform")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    // Infrastructure tidak boleh punya DbContext standalone.
    [Fact]
    public void Ff10_infrastructure_building_block_has_no_standalone_dbcontext()
    {
        var dbContexts = typeof(InfrastructureModelBuilderExtensions).Assembly
            .GetTypes()
            .Where(InheritsDbContext)
            .ToList();

        Assert.Empty(dbContexts);
    }

    private static bool InheritsDbContext(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.FullName == "Microsoft.EntityFrameworkCore.DbContext")
            {
                return true;
            }
        }

        return false;
    }
}
