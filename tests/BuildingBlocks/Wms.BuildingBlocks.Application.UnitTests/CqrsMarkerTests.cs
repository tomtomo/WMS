using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.UnitTests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Marker CQRS
public sealed class CqrsMarkerTests
{
    [Fact]
    public async Task Command_is_dispatched_to_its_handler_and_returns_the_result()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CqrsMarkerTests).Assembly));
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new DoubleValueCommand(21));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }
}
