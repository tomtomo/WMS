using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Persistence;

// Command side tetap menggunakan DbContext, migration, dan mekanisme xmin yang sama, hanya koneksinya yang diarahkan dari PostgreSQL lokal ke Azure Database for PostgreSQL.
// Password diambil dari Key Vault dan koneksi harus memakai TLS.
public sealed class FlexibleServerConnectionStringFactory(
    IConfiguration configuration,
    IOptions<FlexibleServerOptions> options,
    ISecretProvider secretProvider)
{
    public async Task<string> CreateAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var configured = configuration.GetConnectionString(settings.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                $"Connection string '{settings.ConnectionStringName}' untuk Flexible Server tidak ditemukan di konfigurasi.");
        }

        var builder = new NpgsqlConnectionStringBuilder(configured);

        if (!string.IsNullOrWhiteSpace(settings.PasswordSecretName))
        {
            var password = await secretProvider
                .GetSecretAsync(settings.PasswordSecretName, cancellationToken)
                .ConfigureAwait(false);

            // Hentikan startup jika secret password tidak tersedia agar aplikasi tidak mencoba terhubung tanpa kredensial.
            builder.Password = string.IsNullOrWhiteSpace(password)
                ? throw new InvalidOperationException(
                    $"Secret '{settings.PasswordSecretName}' untuk Flexible Server kosong; startup dihentikan.")
                : password;
        }

        if (settings.RequireSsl)
        {
            // Gunakan VerifyFull agar koneksi terenkripsi sekaligus memverifikasi sertifikat dan identitas server.
            builder.SslMode = SslMode.VerifyFull;
        }

        return builder.ConnectionString;
    }
}
