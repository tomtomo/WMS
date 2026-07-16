using System.Net;
using AwesomeAssertions;
using Xunit;

namespace Wms.WebUI.E2ETests;

// Probe harness untuk kriteria 6 (respons 403) dan kriteria 9 (idempotensi REST).
// Keduanya memakai JWT langsung karena sesi Playwright hanya menyimpan cookie.
public sealed class HarnessProbeTests
{
    private const string WarehouseId = "a0000000-0000-0000-0000-000000000001";

    [SkippableFact]
    public async Task Low_perm_gets_403_on_gated_endpoint_admin_does_not()
    {
        var profile = WmsE2EProfile.FromEnvironment();
        Skip.If(!profile.IsConfigured, "WMS_E2E_BASEURL belum di-set — skip (aman di PR).");
        var gateway = profile.GatewayUrl ?? throw new InvalidOperationException("WMS_E2E_GATEWAY_URL wajib untuk probe [HARNESS].");

        using var harness = new HarnessClient(gateway, profile.IgnoreTls);
        var adminToken = await harness.LoginAsync(profile.Username, profile.Password);
        var viewerToken = await harness.LoginAsync(profile.LowPermUsername, profile.LowPermPassword);

        // Create wave membutuhkan permission Outbound.CreateWave.
        var body = new { orderIds = new[] { Guid.NewGuid() }, warehouseId = Guid.Parse(WarehouseId) };

        var viewerStatus = await harness.PostStatusAsync(viewerToken, "/outbound/v1/waves", body);
        viewerStatus.Should().Be(
            HttpStatusCode.Forbidden,
            "viewer low-perm tak punya Outbound.CreateWave → 403 (ADR-0020)");

        var adminStatus = await harness.PostStatusAsync(adminToken, "/outbound/v1/waves", body);
        adminStatus.Should().NotBe(
            HttpStatusCode.Forbidden,
            "admin memegang full catalog → lolos gate (400/404 karena order dummy, BUKAN 403)");
    }

    [SkippableFact]
    public async Task Rest_idempotency_key_replays_without_double_effect()
    {
        var profile = WmsE2EProfile.FromEnvironment();
        Skip.If(!profile.IsConfigured, "WMS_E2E_BASEURL belum di-set — skip (aman di PR).");
        var gateway = profile.GatewayUrl ?? throw new InvalidOperationException("WMS_E2E_GATEWAY_URL wajib untuk probe [HARNESS].");

        using var harness = new HarnessClient(gateway, profile.IgnoreTls);
        var token = await harness.LoginAsync(profile.Username, profile.Password);

        // Kirim request yang sama dua kali; request kedua harus mengembalikan respons pertama tanpa membuat order baru.
        var key = Guid.NewGuid().ToString();
        var body = new
        {
            customerId = Guid.NewGuid(),
            recipient = "Probe Idem",
            addressLine = "Jl. Probe 1",
            city = "Jakarta",
            lines = new[] { new { sku = "SKU-MILK", qty = 10m, uom = "CARTON" } },
        };

        var first = await harness.PostReadAsync<CreateOrderResponse>(token, "/outbound/v1/outbound-orders", body, key);
        var second = await harness.PostReadAsync<CreateOrderResponse>(token, "/outbound/v1/outbound-orders", body, key);

        first.Should().NotBeNull();
        second!.OrderId.Should().Be(
            first!.OrderId,
            "Idempotency-Key sama → replay orderId pertama (no-double-effect, ADR-0017); order kedua TIDAK dibuat");
    }

    private sealed record CreateOrderResponse(Guid OrderId);
}
