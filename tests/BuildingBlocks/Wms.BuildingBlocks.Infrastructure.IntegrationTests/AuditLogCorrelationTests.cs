using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test AuditLogStore mengisi correlation_id dari ICorrelationContext
[Collection(PostgresCollection.Name)]
public sealed class AuditLogCorrelationTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset _auditTime = new(2026, 7, 9, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Audit_row_captures_the_ambient_correlation_id()
    {
        var connectionString = await CreateSchemaAsync();
        await using var provider = BuildProvider(connectionString, new StubCorrelationContext("corr-audit-42"));
        var auditStore = new AuditLogStore(provider.GetRequiredService<IServiceScopeFactory>());

        await auditStore.RecordAsync(new AuditLogEntry("alice", "ApproveReceipt", _auditTime));

        await using var verify = RailContext.New(connectionString);
        var audits = await verify.Set<AuditLogRecord>().ToListAsync();
        audits.Should().ContainSingle();
        audits[0].CorrelationId.Should().Be("corr-audit-42");
    }

    [Fact]
    public async Task Correlation_id_is_null_when_no_context_is_registered()
    {
        var connectionString = await CreateSchemaAsync();
        await using var provider = BuildProvider(connectionString, correlationContext: null);
        var auditStore = new AuditLogStore(provider.GetRequiredService<IServiceScopeFactory>());

        await auditStore.RecordAsync(new AuditLogEntry("bob", "Ship", _auditTime));

        await using var verify = RailContext.New(connectionString);
        var audits = await verify.Set<AuditLogRecord>().ToListAsync();
        audits.Should().ContainSingle();
        audits[0].CorrelationId.Should().BeNull();
    }

    private static ServiceProvider BuildProvider(string connectionString, ICorrelationContext? correlationContext)
    {
        var services = new ServiceCollection();
        services.AddDbContext<RailTestDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<RailTestDbContext>());
        if (correlationContext is not null)
        {
            services.AddScoped(_ => correlationContext);
        }

        return services.BuildServiceProvider();
    }

    private async Task<string> CreateSchemaAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        await using var context = RailContext.New(connectionString);
        await context.Database.EnsureCreatedAsync();
        return connectionString;
    }

    private sealed class StubCorrelationContext(string? correlationId) : ICorrelationContext
    {
        public string? CorrelationId { get; } = correlationId;
    }
}
