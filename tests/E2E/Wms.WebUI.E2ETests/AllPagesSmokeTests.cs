using AwesomeAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Wms.WebUI.E2ETests;

// Buka semua halaman sebagai admin. judul harus benar dan tidak boleh ada page error.
// Pakai ID dummy pada halaman detail untuk test state "tidak ditemukan".
public sealed class AllPagesSmokeTests
{
    private const string DumpDir =
        @"C:\Users\sosro\AppData\Local\Temp\claude\C--Users-sosro-OneDrive-Desktop-TomWorkspace-TomSandboxs-WMS\cc113dc8-5302-4df8-9efc-5ac550bed2a9\scratchpad";

    private const string DummyId = "00000000-0000-0000-0000-000000000009";

    private static readonly (string Route, string Title)[] Pages =
    [
        ("/", "WMS — Home"),
        ("/inbound/goods-receipts", "WMS — Goods Receipts"),
        ($"/inbound/goods-receipts/{DummyId}", "WMS — Goods Receipt Detail"),
        ("/inventory/putaway", "WMS — Putaway"),
        ("/outbound/orders", "WMS — Outbound Orders"),
        ($"/outbound/orders/{DummyId}", "WMS — Order Detail"),
        ("/outbound/waves", "WMS — Waves"),
        ($"/outbound/waves/{DummyId}", "WMS — Wave Detail"),
        ("/outbound/picking", "WMS — Picking"),
        ("/master-data/products", "WMS — Products"),
        ("/master-data/locations", "WMS — Locations"),
        ("/master-data/warehouses", "WMS — Warehouses"),
        ("/reports/stock-on-hand", "WMS — Stock on Hand"),
        ("/reports/dispatch-summary", "WMS — Dispatch Summary"),
        ("/reports/operator-productivity", "WMS — Operator Productivity"),
        ("/reports/supplier-performance", "WMS — Supplier Performance"),
        ("/inbox", "WMS — Inbox"),
    ];

    [SkippableFact]
    public async Task Every_page_renders_with_correct_title_and_no_error()
    {
        var profile = WmsE2EProfile.FromEnvironment();
        Skip.If(!profile.IsConfigured, "WMS_E2E_BASEURL belum di-set — skip (aman di PR).");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true, Channel = profile.BrowserChannel });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = profile.IgnoreTls });

        var login = await context.NewPageAsync();
        await login.GotoAsync($"{profile.BaseUrl}/login");
        await login.FillAsync("input[name=\"username\"]", profile.Username);
        await login.FillAsync("input[name=\"password\"]", profile.Password);
        await login.ClickAsync("button[type=\"submit\"]");
        await login.WaitForURLAsync($"{profile.BaseUrl}/**");
        await login.CloseAsync();

        var page = await context.NewPageAsync();
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);

        var results = new List<string>();
        var failures = new List<string>();

        foreach (var (route, title) in Pages)
        {
            var errorsBefore = pageErrors.Count;
            await page.GotoAsync($"{profile.BaseUrl}{route}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            var titleOk = await TitleMatchesAsync(page, title, 12_000);
            var crashed = pageErrors.Count > errorsBefore;
            results.Add($"{(titleOk && !crashed ? "OK " : "XX ")} {route,-45} title={(titleOk ? "ok" : "MISMATCH")} crash={(crashed ? "YES" : "no")}");

            if (!titleOk)
            {
                failures.Add($"{route}: title != '{title}' (got '{await page.TitleAsync()}')");
            }

            if (crashed)
            {
                failures.Add($"{route}: PAGEERROR — {pageErrors[^1]}");
            }
        }

        Directory.CreateDirectory(DumpDir);
        await File.WriteAllTextAsync(Path.Combine(DumpDir, "e2e-allpages.txt"), string.Join(Environment.NewLine, results));

        failures.Should().BeEmpty(
            $"setiap halaman harus render title benar tanpa crash. Detail:{Environment.NewLine}{string.Join(Environment.NewLine, results)}");
    }

    // PageTitle baru tersedia setelah circuit interaktif aktif, jadi tunggu sampai nilainya cocok.
    private static async Task<bool> TitleMatchesAsync(IPage page, string expected, int timeoutMs)
    {
        try
        {
            await Assertions.Expect(page).ToHaveTitleAsync(expected, new PageAssertionsToHaveTitleOptions { Timeout = timeoutMs });
            return true;
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }
}
