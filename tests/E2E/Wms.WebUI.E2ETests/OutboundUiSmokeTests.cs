using AwesomeAssertions;
using Microsoft.Playwright;
using Xunit;

namespace Wms.WebUI.E2ETests;

// Smoke test UI Outbound melalui alur penuh BFF, gateway, dan backend.
public sealed class OutboundUiSmokeTests
{
    private const string DumpDir =
        @"C:\Users\sosro\AppData\Local\Temp\claude\C--Users-sosro-OneDrive-Desktop-TomWorkspace-TomSandboxs-WMS\cc113dc8-5302-4df8-9efc-5ac550bed2a9\scratchpad";

    [SkippableFact]
    public async Task Admin_login_then_create_order_shows_success()
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

        // Buka halaman Outbound dari page baru agar cookie sesi tetap dipakai.
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

        await page.GotoAsync($"{profile.BaseUrl}/outbound/orders");
        var createButton = page.Locator("button:has-text(\"Buat Order\")");
        await createButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 20_000 });

        var created = false;
        for (var attempt = 0; attempt < 10 && !created; attempt++)
        {
            await page.WaitForTimeoutAsync(1200);
            await createButton.ClickAsync();
            created = await WaitForTextAsync(page, "Order dibuat.", 2500);
        }

        await File.WriteAllTextAsync(Path.Combine(DumpDir, "e2e-console.txt"), string.Join("\n", console));
        await File.WriteAllTextAsync(Path.Combine(DumpDir, "e2e-posts.txt"), string.Join("\n", posts));

        created.Should().BeTrue("snackbar sukses muncul setelah create order menembus full-stack BFF→gateway→backend");
    }

    private static async Task<bool> WaitForTextAsync(IPage page, string text, int timeoutMs)
    {
        try
        {
            await page.GetByText(text).WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
