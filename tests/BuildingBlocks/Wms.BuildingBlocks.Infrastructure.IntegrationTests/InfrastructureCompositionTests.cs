using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test composition root AddBuildingBlocksInfrastructure
public sealed class InfrastructureCompositionTests
{
    [Fact]
    public void Resolves_every_infrastructure_service_with_validated_scopes()
    {
        var services = new ServiceCollection();
        services.AddDbContext<RailTestDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=wms;Username=postgres;Password=postgres"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<RailTestDbContext>());
        services.AddSingleton(Substitute.For<ICurrentUser>());

        services.AddBuildingBlocksInfrastructure("wms-tests");

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var resolver = scope.ServiceProvider;

        resolver.GetService<IUnitOfWork>().Should().NotBeNull();
        resolver.GetService<IInboxGuard>().Should().NotBeNull();
        resolver.GetService<IIntegrationEventOutbox>().Should().NotBeNull();
        resolver.GetService<IDeadLetterStore>().Should().NotBeNull();
        resolver.GetService<IAuditLogStore>().Should().NotBeNull();
        resolver.GetService<ITelemetrySink>().Should().NotBeNull();
        resolver.GetService<AuditableInterceptor>().Should().NotBeNull();
        resolver.GetService<ConsumerDeadLetterPipeline>().Should().NotBeNull();
    }
}
