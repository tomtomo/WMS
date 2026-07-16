using AwesomeAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Wms.WebUI.E2ETests;

// Pastikan route berproteksi tidak bisa diakses tanpa login sesuai kriteria 5.
// Test ini memakai pengguna anonim, bukan pengguna terautentikasi dengan permission terbatas.
public sealed class NegativeAuthTests
{
    [SkippableFact]
    public async Task Gated_route_without_login_hides_actions_and_offers_login()
    {
        var profile = WmsE2EProfile.FromEnvironment();
        Skip.If(!profile.IsConfigured, "WMS_E2E_BASEURL belum di-set — skip (aman di PR).");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true, Channel = profile.BrowserChannel });

        // Context baru tidak membawa cookie sesi, jadi pengguna tetap anonim.
        var context = await browser.NewContextAsync(
            new BrowserNewContextOptions { IgnoreHTTPSErrors = profile.IgnoreTls });
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{profile.BaseUrl}/outbound/orders");
        await page.WaitForTimeoutAsync(2000);

        var createButtonCount = await page.Locator("button:has-text(\"Buat Order\")").CountAsync();
        createButtonCount.Should().Be(
            0,
            "anonymous tak boleh melihat aksi gated Buat Order (negative-auth, bukan tombol-403)");

        var loginLinkCount = await page.Locator("a[href='/login']").CountAsync();
        loginLinkCount.Should().BeGreaterThan(
            0,
            "anonymous ditawari login lewat nav (degradasi auth yang benar)");
    }
}
