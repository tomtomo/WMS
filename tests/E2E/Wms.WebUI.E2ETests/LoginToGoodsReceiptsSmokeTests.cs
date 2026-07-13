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
        var baseUrl = Environment.GetEnvironmentVariable("WMS_E2E_BASEURL")?.TrimEnd('/');
        Skip.If(string.IsNullOrEmpty(baseUrl), "Set WMS_E2E_BASEURL ke WebUI yang jalan untuk menjalankan smoke E2E.");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();

        // Login lokal (admin seed).
        await page.GotoAsync($"{baseUrl}/login");
        await page.FillAsync("input[name=\"username\"]", "admin");
        await page.FillAsync("input[name=\"password\"]", "ChangeMe#2026");
        await page.ClickAsync("button[type=\"submit\"]");
        await page.WaitForURLAsync($"{baseUrl}/**");

        // Pastikan halaman Goods Receipts dapat dibuka setelah login dan mengambil data melalui gateway.
        await page.GotoAsync($"{baseUrl}/inbound/goods-receipts");
        await page.WaitForSelectorAsync("text=Goods Receipts");

        (await page.ContentAsync()).Should().Contain("Goods Receipts").And.Contain("Pending Review");
    }
}
