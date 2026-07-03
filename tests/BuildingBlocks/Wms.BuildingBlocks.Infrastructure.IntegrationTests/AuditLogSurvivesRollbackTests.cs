using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test AuditLogStore
public sealed class AuditLogSurvivesRollbackTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset _auditTime = new(2026, 7, 3, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Audit_row_is_written_even_when_the_business_transaction_rolls_back()
    {
        var connectionString = await CreateSchemaAsync();
        await using var provider = BuildProvider(connectionString);
        var auditStore = new AuditLogStore(provider.GetRequiredService<IServiceScopeFactory>());

        await using (var businessScope = provider.CreateAsyncScope())
        {
            var businessContext = businessScope.ServiceProvider.GetRequiredService<RailTestDbContext>();
            await using var transaction = await businessContext.Database.BeginTransactionAsync();
            businessContext.Set<WidgetEntity>().Add(new WidgetEntity { Id = Guid.NewGuid(), Name = "doomed" });
            await businessContext.SaveChangesAsync();

            await auditStore.RecordAsync(new AuditLogEntry("alice", "ApproveReceipt", _auditTime));

            await transaction.RollbackAsync();
        }

        await using var verify = RailContext.New(connectionString);
        (await verify.Set<WidgetEntity>().CountAsync()).Should().Be(0);
        var audits = await verify.Set<AuditLogRecord>().ToListAsync();
        audits.Should().ContainSingle();
        audits[0].Actor.Should().Be("alice");
        audits[0].Action.Should().Be("ApproveReceipt");
    }

    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<RailTestDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<RailTestDbContext>());
        return services.BuildServiceProvider();
    }

    private async Task<string> CreateSchemaAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        await using var context = RailContext.New(connectionString);
        await context.Database.EnsureCreatedAsync();
        return connectionString;
    }
}
