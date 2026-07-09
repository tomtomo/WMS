using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Wms.WebUI.Bff;

// BFF menyimpan token di server dan browser hanya memegang cookie HttpOnly. Request ke gateway otomatis diberi bearer token dan correlation id.
public static class BffExtensions
{
    public const string GatewayClientName = "gateway";

    public static IServiceCollection AddWebUiBff(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();
        services.AddSingleton<ITokenStore, InMemoryTokenStore>();
        services.AddTransient<BearerForwardingHandler>();

        var gatewayAddress = new Uri(
            configuration["Bff:GatewayAddress"]
            ?? throw new InvalidOperationException("Konfigurasi 'Bff:GatewayAddress' wajib ada (di-inject AppHost)."));
        services.AddInternalHttpClient(GatewayClientName, gatewayAddress)
            .AddHttpMessageHandler<BearerForwardingHandler>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.LoginPath = "/login";
            });
        services.AddAuthorization();
        services.AddAntiforgery();

        return services;
    }

    public static void MapBffEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var bff = app.MapGroup("/bff");

        // Login memakai form post biasa supaya cookie bisa diset dari response.
        // SameSite=Strict membantu menahan request lintas site.
        bff.MapPost("/login", LoginAsync).DisableAntiforgery();
        bff.MapPost("/logout", LogoutAsync);
    }

    private static async Task<IResult> LoginAsync(
        HttpContext httpContext,
        IHttpClientFactory httpClientFactory,
        ITokenStore tokenStore,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var credentials = new LoginRequest(form["username"].ToString(), form["password"].ToString());

        var client = httpClientFactory.CreateClient(GatewayClientName);

        // Gateway menerima route /auth/v1/login, lalu meneruskannya ke Auth host sebagai /v1/login.
        var response = await client.PostAsJsonAsync("/auth/v1/login", credentials, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Results.Redirect("/login?error=1");
        }

        var token = await response.Content.ReadFromJsonAsync<GatewayTokenResponse>(cancellationToken);
        if (token is null)
        {
            return Results.Redirect("/login?error=1");
        }

        // JWT disimpan server-side. Browser hanya menerima session cookie HttpOnly.
        var sessionId = Guid.NewGuid().ToString("N");
        tokenStore.Set(sessionId, token.AccessToken);

        var identity = new ClaimsIdentity(
            [new Claim(BffClaims.SessionId, sessionId), new Claim(ClaimTypes.Name, credentials.Username)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Results.Redirect("/");
    }

    private static async Task<IResult> LogoutAsync(ITokenStore tokenStore, HttpContext httpContext)
    {
        var sessionId = httpContext.User.FindFirst(BffClaims.SessionId)?.Value;
        if (sessionId is not null)
        {
            tokenStore.Remove(sessionId);
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }
}

// Kredensial dikirim dari form login WebUI ke BFF. Token tidak pernah dikirim balik ke browser.
public sealed record LoginRequest(string Username, string Password);

// Respons token dari gateway dibaca di server dan disimpan di token store.
internal sealed record GatewayTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);

internal static class BffClaims
{
    public const string SessionId = "wms.bff.session";
}
