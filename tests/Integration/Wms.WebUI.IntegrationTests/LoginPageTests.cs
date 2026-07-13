using AwesomeAssertions;
using Wms.WebUI.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// Pastikan tombol login Microsoft hanya muncul saat Entra dikonfigurasi, tanpa memengaruhi login lokal.
public sealed class LoginPageTests
{
    [Fact]
    public async Task Login_page_shows_only_local_login_when_entra_is_not_configured()
    {
        await using var factory = new WebUiFactory("http://127.0.0.1:1");
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/login");

        html.Should().Contain("name=\"username\"", "login lokal selalu tersedia");
        html.Should().NotContain("Sign in with Microsoft");
    }

    [Fact]
    public async Task Login_page_shows_the_microsoft_button_when_entra_is_configured()
    {
        await using var factory = new WebUiFactory("http://127.0.0.1:1", enableEntra: true);
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/login");

        html.Should().Contain("Sign in with Microsoft");
        html.Should().Contain("/bff/login/entra");
        html.Should().Contain("name=\"username\"", "dua tombol hidup berdampingan");
    }
}
