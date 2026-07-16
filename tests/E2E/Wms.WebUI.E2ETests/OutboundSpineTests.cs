using System.Globalization;
using AwesomeAssertions;
using Microsoft.Playwright;
using Npgsql;
using Xunit;

namespace Wms.WebUI.E2ETests;

// E2E utama dari login sampai ShipmentDispatched. Data GR disiapkan lewat harness sebagai prasyarat.
// Alur outbound lengkap—order, wave, picking, dan dispatch tetap dijalankan lewat UI.
// Urutan 11 event dibuktikan dari outbox. penyimpangan saat seed dicatat di laporan.
public sealed class OutboundSpineTests
{
    private const string WarehouseId = "a0000000-0000-0000-0000-000000000001";
    private const string PickerId = "c0000000-0000-0000-0000-000000000001";
    private const string Sku = "SKU-MILK";

    private const string DumpDir =
        @"C:\Users\sosro\AppData\Local\Temp\claude\C--Users-sosro-OneDrive-Desktop-TomWorkspace-TomSandboxs-WMS\cc113dc8-5302-4df8-9efc-5ac550bed2a9\scratchpad";

    // Daftar 11 event yang harus muncul dalam urutan
    private static readonly string[] SpineChain =
    [
        "inbound.goods_receipt_pending_review.v1",
        "inbound.gr_confirmed.v1",
        "inventory.putaway_task_assigned.v1",
        "inventory.putaway_completed.v1",
        "outbound.wave_released.v1",
        "inventory.stock_allocation_completed.v1",
        "outbound.picking_task_assigned.v1",
        "outbound.picking_completed.v1",
        "outbound.wave_ready.v1",
        "outbound.shipment_dispatched.v1",
        "inventory.stock_removed.v1",
    ];

    [SkippableFact]
    public async Task Login_to_shipment_dispatched_via_ui_with_event_chain_evidence()
    {
        var profile = WmsE2EProfile.FromEnvironment();
        Skip.If(!profile.IsConfigured, "WMS_E2E_BASEURL belum di-set — skip (aman di PR).");
        var gateway = profile.GatewayUrl
            ?? throw new InvalidOperationException("WMS_E2E_GATEWAY_URL wajib untuk spine (seed + evidence).");

        // Timestamp dan referensi unik mencegah hasil run lama di persistent volume ikut terbaca.
        var since = DateTimeOffset.UtcNow.AddSeconds(-5);
        var stamp = since.Ticks.ToString(CultureInfo.InvariantCulture);

        using var harness = new HarnessClient(gateway, profile.IgnoreTls);
        var token = await harness.LoginAsync(profile.Username, profile.Password);

        await SeedGoodsReceiptOkAsync(harness, token, stamp);
        var (orderId, picking) = await DriveOutboundFullViaUiAsync(profile, harness, token);
        await AssertOrderClosedAsync(profile, orderId);
        await AssertEventChainAsync(profile, since);

        // Picking hanya dipakai untuk menemukan wave; hasil akhirnya tetap order Closed dan rantai event lengkap.
        picking.WaveId.Should().NotBeEmpty();
    }

    // siapkan GR OK lewat harness sampai stok 100 berstatus Available.
    private static async Task SeedGoodsReceiptOkAsync(HarnessClient harness, string token, string stamp)
    {
        var created = await harness.PostReadAsync<CreateGrResponse>(
            token,
            "/inbound/v1/goods-receipts",
            new
            {
                poRef = $"E2E-{stamp}",
                supplierId = Guid.NewGuid(),
                warehouseId = Guid.Parse(WarehouseId),
                dockDoor = "D-E2E",
                expectedLines = new[] { new { sku = Sku, expectedQty = 100m, uom = "CARTON" } },
            });
        var grId = created!.GoodsReceiptId;

        var putawayBefore = await PutawayIdsAsync(harness, token);
        await harness.PostOkAsync(
            token,
            $"/inbound/v1/goods-receipts/{grId}/scans",
            new
            {
                sku = Sku,
                actualQty = 100m,
                batch = $"B{stamp}",
                expiry = new DateOnly(2027, 12, 31),
                lineStatus = "Good",
            });
        await harness.PostOkAsync(token, $"/inbound/v1/goods-receipts/{grId}/complete-scan");
        await harness.PostOkAsync(token, $"/inbound/v1/goods-receipts/{grId}/confirm");

        // GRConfirmed membuat stok dan putaway task secara async; tunggu task baru sebelum diselesaikan.
        var putaway = await PollForAsync(
            () => harness.GetJsonAsync<List<PutawayDto>>(token, "/inventory/v1/putaway-tasks"),
            task => !putawayBefore.Contains(task.PutawayTaskId),
            "putaway task baru pasca-GRConfirmed");
        await harness.PostOkAsync(
            token,
            $"/inventory/v1/putaway-tasks/{putaway.PutawayTaskId}/complete",
            new { actualDestinationId = putaway.SuggestedDestinationId, operatorId = (Guid?)null });
    }

    // jalankan alur outbound lewat UI. Harness hanya dipakai untuk menemukan entitas yang dibuat.
    private static async Task<(Guid OrderId, PickingDto Picking)> DriveOutboundFullViaUiAsync(
        WmsE2EProfile profile,
        HarnessClient harness,
        string token)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true, Channel = profile.BrowserChannel });
        var context = await browser.NewContextAsync(
            new BrowserNewContextOptions { IgnoreHTTPSErrors = profile.IgnoreTls });

        var loginPage = await context.NewPageAsync();
        await loginPage.GotoAsync($"{profile.BaseUrl}/login");
        await loginPage.FillAsync("input[name=\"username\"]", profile.Username);
        await loginPage.FillAsync("input[name=\"password\"]", profile.Password);
        await loginPage.ClickAsync("button[type=\"submit\"]");
        await loginPage.WaitForURLAsync($"{profile.BaseUrl}/**");
        await loginPage.CloseAsync();

        var page = await context.NewPageAsync();
        var console = new List<string>();
        var posts = new List<string>();
        page.Console += (_, message) => console.Add($"{message.Type}: {message.Text}");
        page.PageError += (_, error) => console.Add($"PAGEERROR: {error}");
        page.Request += (_, request) =>
        {
            if (string.Equals(request.Method, "POST", StringComparison.Ordinal))
            {
                posts.Add(request.Url);
            }
        };

        // buat order lewat UI, lalu cari order baru dari selisih backlog.
        var ordersBefore = await BacklogIdsAsync(harness, token);
        await page.GotoAsync($"{profile.BaseUrl}/outbound/orders");
        await DriveAsync(page, () => page.Locator("button:has-text(\"Buat Order\")"), "Order dibuat.");
        var order = await PollForAsync(
            () => harness.GetJsonAsync<List<BacklogDto>>(token, "/outbound/v1/outbound-orders/backlog"),
            candidate => !ordersBefore.Contains(candidate.OrderId),
            "order baru di backlog");

        // buat wave untuk order ini, lalu tunggu alokasi menghasilkan picking task.
        var pickingBefore = await PickingIdsAsync(harness, token);
        await page.GotoAsync($"{profile.BaseUrl}/outbound/waves");
        await DriveAsync(
            page,
            () => page.Locator($"tr:has-text(\"{order.OrderId}\")").Locator("button:has-text(\"Buat Wave\")"),
            "Wave dibuat.");

        // tunggu picking task baru dari alokasi penuh untuk menemukan wave run ini.
        var picking = await PollForAsync(
            () => harness.GetJsonAsync<List<PickingDto>>(token, $"/outbound/v1/picking-tasks?assignedTo={PickerId}"),
            task => !pickingBefore.Contains(task.PickingTaskId),
            "picking task baru pasca-alokasi");

        // selesaikan picking lewat UI sampai wave berstatus Ready.
        await page.GotoAsync($"{profile.BaseUrl}/outbound/picking");
        await DriveAsync(
            page,
            () => page.Locator($"tr:has-text(\"{picking.PickingTaskId}\")").Locator("button:has-text(\"Complete\")"),
            "Picking selesai.");
        await PollForAsync(
            () => harness.GetJsonAsync<List<WaveDto>>(token, $"/outbound/v1/waves?warehouseId={WarehouseId}&status=Ready"),
            wave => wave.WaveId == picking.WaveId,
            "wave transisi ke Ready");

        // dispatch wave lewat UI sampai order berstatus Closed.
        await page.GotoAsync($"{profile.BaseUrl}/outbound/waves");
        await DriveAsync(
            page,
            () => page.Locator($"tr:has-text(\"{picking.WaveId}\")").Locator("button:has-text(\"Dispatch\")"),
            "Wave di-dispatch.");

        Directory.CreateDirectory(DumpDir);
        await File.WriteAllTextAsync(
            Path.Combine(DumpDir, "e2e-spine-console.txt"),
            string.Join(Environment.NewLine, console));
        await File.WriteAllTextAsync(
            Path.Combine(DumpDir, "e2e-spine-posts.txt"),
            string.Join(Environment.NewLine, posts));

        return (order.OrderId, picking);
    }

    // aggregate order harus benar-benar Closed, bukan sekadar hilang dari backlog.
    // Penutupan terjadi async setelah dispatch, jadi tunggu statusnya langsung dari database Outbound.
    private static async Task AssertOrderClosedAsync(WmsE2EProfile profile, Guid orderId)
    {
        var connString = profile.DbFor("wms_outbound")
            ?? throw new InvalidOperationException("WMS_E2E_DB_CONNSTRING wajib untuk assert order Closed.");

        for (var attempt = 0; attempt < 40; attempt++)
        {
            await using var connection = new NpgsqlConnection(connString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "SELECT status FROM outbound.outbound_order WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", orderId);
            var status = await command.ExecuteScalarAsync() as string;
            if (string.Equals(status, "Closed", StringComparison.Ordinal))
            {
                return;
            }

            await Task.Delay(1500);
        }

        throw new TimeoutException("order tidak transisi ke Closed (~60s).");
    }

    // pastikan 11 event outbox muncul dalam urutan kausal.
    private static async Task AssertEventChainAsync(WmsE2EProfile profile, DateTimeOffset since)
    {
        var reader = new EventLogReader(profile);
        var rows = await reader.ReadSpineEventsAsync(since);
        var chain = rows.Select(row => row.LogicalName).ToList();

        Directory.CreateDirectory(DumpDir);
        await File.WriteAllTextAsync(
            Path.Combine(DumpDir, "e2e-spine-events.txt"),
            string.Join(
                Environment.NewLine,
                rows.Select(row => $"{row.OccurredAt:O}  {row.Module,-14} {row.LogicalName}  trace={row.Traceparent}")));

        foreach (var name in SpineChain)
        {
            chain.Should().Contain(name, $"event spine '{name}' harus fired di run ini");
        }

        // Cukup cek urutan parent-child; event sibling boleh saling mendahului.
        AssertBefore(chain, "inbound.goods_receipt_pending_review.v1", "inbound.gr_confirmed.v1");
        AssertBefore(chain, "inbound.gr_confirmed.v1", "inventory.putaway_task_assigned.v1");
        AssertBefore(chain, "inventory.putaway_task_assigned.v1", "inventory.putaway_completed.v1");
        AssertBefore(chain, "inventory.putaway_completed.v1", "outbound.wave_released.v1");
        AssertBefore(chain, "outbound.wave_released.v1", "inventory.stock_allocation_completed.v1");
        AssertBefore(chain, "inventory.stock_allocation_completed.v1", "outbound.picking_task_assigned.v1");
        AssertBefore(chain, "outbound.picking_task_assigned.v1", "outbound.picking_completed.v1");
        AssertBefore(chain, "outbound.picking_completed.v1", "outbound.wave_ready.v1");
        AssertBefore(chain, "outbound.wave_ready.v1", "outbound.shipment_dispatched.v1");
        AssertBefore(chain, "outbound.shipment_dispatched.v1", "inventory.stock_removed.v1");
    }

    // Blazor Server kadang belum interaktif saat halaman terbuka, jadi ulangi klik sampai snackbar sukses.
    // Hasil tetap divalidasi lewat harness; snackbar hanya mencegah request terkirim dua kali.
    private static async Task DriveAsync(IPage page, Func<ILocator> locatorFactory, string successText)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            // Periksa snackbar dari percobaan sebelumnya sebelum mengklik lagi.
            if (await WaitForTextAsync(page, successText, attempt == 0 ? 300 : 800))
            {
                return;
            }

            var locator = locatorFactory();
            if (await locator.CountAsync() == 0)
            {
                // Tombol mungkin belum muncul atau baris sudah hilang setelah sukses; tunggu lalu cek ulang.
                await page.WaitForTimeoutAsync(1000);
                continue;
            }

            try
            {
                await locator.First.ClickAsync(new LocatorClickOptions { Timeout = 2500 });
            }
            catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
            {
                await page.WaitForTimeoutAsync(500);
            }
        }
    }

    // Playwright .NET melempar System.TimeoutException saat locator kehabisan waktu.
    private static async Task<bool> WaitForTextAsync(IPage page, string text, int timeoutMs)
    {
        try
        {
            await page.GetByText(text).First.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    // Cek harness berulang sampai menemukan item yang cocok dengan entitas milik run ini.
    private static async Task<T> PollForAsync<T>(Func<Task<List<T>?>> fetch, Func<T, bool> predicate, string what)
        where T : class
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var items = await fetch() ?? [];
            var match = items.FirstOrDefault(predicate);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(1500);
        }

        throw new TimeoutException($"Timeout menunggu: {what} (~60s).");
    }

    private static void AssertBefore(List<string> chain, string parent, string child) =>
        chain.IndexOf(parent).Should().BeLessThan(
            chain.IndexOf(child),
            $"kausal: '{parent}' harus mendahului '{child}'");

    private static async Task<HashSet<Guid>> PutawayIdsAsync(HarnessClient harness, string token)
    {
        var tasks = await harness.GetJsonAsync<List<PutawayDto>>(token, "/inventory/v1/putaway-tasks") ?? [];
        return [.. tasks.Select(task => task.PutawayTaskId)];
    }

    private static async Task<HashSet<Guid>> BacklogIdsAsync(HarnessClient harness, string token)
    {
        var orders = await harness.GetJsonAsync<List<BacklogDto>>(token, "/outbound/v1/outbound-orders/backlog") ?? [];
        return [.. orders.Select(order => order.OrderId)];
    }

    private static async Task<HashSet<Guid>> PickingIdsAsync(HarnessClient harness, string token)
    {
        var tasks = await harness.GetJsonAsync<List<PickingDto>>(token, $"/outbound/v1/picking-tasks?assignedTo={PickerId}") ?? [];
        return [.. tasks.Select(task => task.PickingTaskId)];
    }

    private sealed record CreateGrResponse(Guid GoodsReceiptId);

    private sealed record PutawayDto(Guid PutawayTaskId, Guid SuggestedDestinationId);

    private sealed record BacklogDto(Guid OrderId);

    private sealed record PickingDto(Guid PickingTaskId, Guid WaveId);

    private sealed record WaveDto(Guid WaveId, string Status);
}
