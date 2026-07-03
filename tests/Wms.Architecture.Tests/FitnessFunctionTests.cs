using NetArchTest.Rules;
using Wms.BuildingBlocks.Application;
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
}
