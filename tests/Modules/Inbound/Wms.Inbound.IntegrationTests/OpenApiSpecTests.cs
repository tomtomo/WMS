using System.Runtime.CompilerServices;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Api.Endpoints;
using Wms.Inbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inbound.IntegrationTests;

// Generate dan validasi OpenAPI Inbound v1 yang di-import ke APIM.
// File spec hanya diperbarui saat WMS_REGEN_OPENAPI=1, menggunakan host minimal tanpa database atau worker.
public sealed class OpenApiSpecTests
{
    [Fact]
    public async Task Inbound_v1_openapi_generates_with_goods_receipt_paths()
    {
        var json = await GenerateAsync();

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("openapi").GetString().Should().StartWith("3.");
        document.RootElement.GetProperty("paths").EnumerateObject().Should().NotBeEmpty();
        json.Should().Contain("/v1/goods-receipts");

        if (Environment.GetEnvironmentVariable("WMS_REGEN_OPENAPI") == "1")
        {
            await File.WriteAllTextAsync(ArtifactPath(), json);
        }
    }

    private static async Task<string> GenerateAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        // Gunakan komposisi DI lengkap agar parameter endpoint dikenali sebagai service saat OpenAPI dibuat.
        // Dependency test dipakai agar proses generate tidak membuka koneksi database atau menjalankan worker.
        InboundTestHost.AddInboundComposition(builder.Services, "Host=localhost;Database=wms;Username=openapi;Password=openapi");
        builder.Services.AddSingleton<IApiIdempotencyStore>(new InMemoryIdempotencyStore());
        builder.Services.AddWebBuildingBlocks();

        var app = builder.Build();
        app.UseSwagger();
        app.MapEndpoints(typeof(CreateGoodsReceiptEndpoint).Assembly);

        await app.StartAsync();
        try
        {
            return await app.GetTestClient().GetStringAsync("/swagger/v1/swagger.json");
        }
        finally
        {
            await app.StopAsync();
        }
    }

    // Tentukan lokasi file OpenAPI dari lokasi source file agar tetap konsisten di setiap environment.
    private static string ArtifactPath([CallerFilePath] string thisFile = "")
    {
        var testDir = Path.GetDirectoryName(thisFile)!;
        var repoRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "deploy", "azure", "openapi", "inbound-v1.json");
    }
}
