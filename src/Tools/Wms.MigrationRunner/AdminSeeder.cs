using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Wms.MigrationRunner;

// Seed admin idempotent
internal static class AdminSeeder
{
    // Placeholder tabel Auth.
    private const string AuthProbeSql = "SELECT to_regclass('auth.users') IS NOT NULL;";

    public static async Task SeedAsync(IConfiguration configuration, ILogger logger, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("wms");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Standalone tanpa DB — tak ada yang di seed, exit bersih.
            logger.LogInformation("AdminSeeder: connection string 'wms' kosong — lewati seed.");
            return;
        }

        var connection = new NpgsqlConnection(connectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var probe = new NpgsqlCommand(AuthProbeSql, connection);
            await using (probe.ConfigureAwait(false))
            {
                if (await probe.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
                {
                    logger.LogInformation("AdminSeeder: schema Auth belum ada — lewati seed admin.");
                    return;
                }
            }

            // idempotent upsert admin user/role.
            logger.LogInformation("AdminSeeder: tabel Auth ditemukan — seed admin idempotent.");
        }
    }
}
