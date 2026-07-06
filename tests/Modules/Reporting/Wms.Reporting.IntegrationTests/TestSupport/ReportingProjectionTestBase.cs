using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Reporting.Persistence;
using Xunit;

namespace Wms.Reporting.IntegrationTests.TestSupport;

// Base test projection
public abstract class ReportingProjectionTestBase(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = ReportingTestHost.Build(connectionString);
        await ReportingTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    // Satu delivery = satu scope
    protected async Task<Result> DeliverAsync<TConsumer>(Func<TConsumer, Task<Result>> consume)
        where TConsumer : notnull
    {
        ArgumentNullException.ThrowIfNull(consume);
        using var scope = _provider.CreateScope();
        return await consume(scope.ServiceProvider.GetRequiredService<TConsumer>());
    }

    // Read projection state di scope terpisah.
    protected async Task<T> QueryAsync<T>(Func<ReportingDbContext, Task<T>> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        using var scope = _provider.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<ReportingDbContext>());
    }

    // Resolve service lain di scope tersendiri.
    protected async Task<T> ScopedAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var scope = _provider.CreateScope();
        return await action(scope.ServiceProvider);
    }
}
