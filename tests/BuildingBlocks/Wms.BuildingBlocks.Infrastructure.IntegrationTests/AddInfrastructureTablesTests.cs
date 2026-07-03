using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test AddInfrastructureTables
[Collection(PostgresCollection.Name)]
public sealed class AddInfrastructureTablesTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Maps_exactly_the_four_rail_tables_in_the_infrastructure_schema()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        await using var context = new RailTestDbContext(
            new DbContextOptionsBuilder<RailTestDbContext>().UseNpgsql(connectionString).Options);

        await context.Database.EnsureCreatedAsync();

        var tables = await QueryTableNamesAsync(connectionString, "infrastructure");

        tables.Should().BeEquivalentTo("outbox", "inbox", "dead_letter", "audit_log");
    }

    private static async Task<List<string>> QueryTableNamesAsync(string connectionString, string schema)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "select table_name from information_schema.tables where table_schema = @schema";
        command.Parameters.AddWithValue("schema", schema);

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
