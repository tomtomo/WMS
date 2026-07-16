using AwesomeAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Wms.WebUI.E2ETests;

// Test alur login lokal hingga halaman Goods Receipts terbuka pada WebUI yang sedang berjalan.
public sealed class LoginToGoodsReceiptsSmokeTests
{
    [SkippableFact]
    public async Task Login_then_open_goods_receipts_page()
    {
        var profile = WmsE2EProfile.FromEnvironment();
        Skip.If(!profile.IsConfigured, "Set WMS_E2E_BASEURL ke WebUI yang jalan untuk menjalankan smoke E2E.");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true, Channel = profile.BrowserChannel });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = profile.IgnoreTls });
        var page = await context.NewPageAsync();

        // Login lokal (admin seed).
        await page.GotoAsync($"{profile.BaseUrl}/login");
        await page.FillAsync("input[name=\"username\"]", profile.Username);
        await page.FillAsync("input[name=\"password\"]", profile.Password);
        await page.ClickAsync("button[type=\"submit\"]");
        await page.WaitForURLAsync($"{profile.BaseUrl}/**");

        // Pastikan halaman Goods Receipts dapat dibuka setelah login dan mengambil data melalui gateway.
        await page.GotoAsync($"{profile.BaseUrl}/inbound/goods-receipts");
        await page.WaitForSelectorAsync("text=Goods Receipts");

        (await page.ContentAsync()).Should().Contain("Goods Receipts").And.Contain("Pending Review");
    }
}
