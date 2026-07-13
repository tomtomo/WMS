using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Wms.BuildingBlocks.Infrastructure.Resilience;

namespace Wms.WebUI.Bff;

// Browser hanya menyimpan cookie HttpOnly. Token internal dan profil Entra tetap disimpan di server, sedangkan API hanya menerima token internal.
public static class BffExtensions
{
    public const string GatewayClientName = "gateway";

    public const string GraphClientName = "graph";

    private const string EntraScheme = "EntraOidc";

    public static IServiceCollection AddWebUiBff(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();
        services.AddSingleton<ITokenStore, InMemoryTokenStore>();
        services.AddSingleton<IUserProfileStore, InMemoryUserProfileStore>();
        services.AddTransient<BearerForwardingHandler>();

        var gatewayAddress = new Uri(
            configuration["Bff:GatewayAddress"]
            ?? throw new InvalidOperationException("Konfigurasi 'Bff:GatewayAddress' wajib ada (di-inject AppHost)."));
        services.AddInternalHttpClient(GatewayClientName, gatewayAddress)
            .AddHttpMessageHandler<BearerForwardingHandler>();

        var entra = new EntraBffOptions();
        configuration.GetSection(EntraBffOptions.SectionName).Bind(entra);
        services.AddSingleton(entra);

        // Auth state Blazor (AuthorizeView) dari cookie.
        services.AddScoped<AuthenticationStateProvider, BffAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();

        var authentication = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
        authentication.AddCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/login";
        });

        // Daftarkan login Entra hanya saat dikonfigurasi agar login lokal tetap berjalan tanpa konfigurasi tambahan.
        if (entra.Enabled)
        {
            // Gunakan timeout, retry, dan circuit breaker untuk panggilan ke Microsoft Graph tanpa menduplikasi handler bawaan.
#pragma warning disable EXTEXP0001
            services.AddHttpClient(GraphClientName, client =>
                    client.BaseAddress = new Uri(entra.GraphBaseAddress))
                .RemoveAllResilienceHandlers()
                .AddHttpResilience();
#pragma warning restore EXTEXP0001
            authentication.AddOpenIdConnect(EntraScheme, options => ConfigureEntra(options, entra));
        }

        services.AddAuthorization();
        services.AddAntiforgery();

        return services;
    }

    public static void MapBffEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var bff = app.MapGroup("/bff");

        // Login lokal memakai form post agar cookie dibuat dari response, sekaligus memblokir request lintas situs melalui SameSite=Strict.
        bff.MapPost("/login", LoginAsync).DisableAntiforgery();

        // Mulai proses login Microsoft dengan mengarahkan pengguna ke Entra ID.
        bff.MapGet("/login/entra", EntraChallenge);

        bff.MapPost("/logout", LogoutAsync).DisableAntiforgery();
    }

    private static void ConfigureEntra(OpenIdConnectOptions options, EntraBffOptions entra)
    {
        options.Authority = entra.Authority;
        options.ClientId = entra.ClientId;
        options.ClientSecret = entra.ClientSecret;
        options.CallbackPath = entra.CallbackPath;
        options.SignedOutCallbackPath = entra.SignedOutCallbackPath;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;

        // Token diambil langsung saat validasi, jadi tidak perlu disimpan oleh middleware OIDC.
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("User.Read");

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = OnEntraTokenValidatedAsync,
            OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/login?error=entra");
                return Task.CompletedTask;
            },
        };
    }

    // Tukarkan token Entra dengan JWT internal, lalu isi cookie dengan klaim sesi dan nama pengguna.
    private static async Task OnEntraTokenValidatedAsync(TokenValidatedContext context)
    {
        var services = context.HttpContext.RequestServices;
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var tokenStore = services.GetRequiredService<ITokenStore>();
        var profileStore = services.GetRequiredService<IUserProfileStore>();

        var idToken = context.TokenEndpointResponse?.IdToken;
        if (string.IsNullOrEmpty(idToken))
        {
            context.Fail("id_token Entra tak diterima.");
            return;
        }

        var gateway = httpClientFactory.CreateClient(GatewayClientName);
        using var response = await gateway.PostAsJsonAsync("/auth/v1/login/entra", new { idToken });
        if (!response.IsSuccessStatusCode)
        {
            // Gagalkan login jika token tidak valid atau akun Entra belum terhubung ke pengguna WMS.
            context.Fail("Identitas Entra belum ditautkan ke user WMS.");
            return;
        }

        var token = await response.Content.ReadFromJsonAsync<GatewayTokenResponse>();
        if (token is null)
        {
            context.Fail("Respons token gateway kosong.");
            return;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        tokenStore.Set(sessionId, token.AccessToken);

        var profile = await FetchGraphProfileAsync(httpClientFactory, context.TokenEndpointResponse?.AccessToken);
        profileStore.Set(sessionId, profile);

        var displayName = profile.DisplayName ?? context.Principal?.Identity?.Name ?? "Entra user";
        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(BffClaims.SessionId, sessionId), new Claim(ClaimTypes.Name, displayName)],
            CookieAuthenticationDefaults.AuthenticationScheme));
    }

    // Ambil profil dan foto pengguna dari Microsoft Graph tanpa menggagalkan login jika permintaan gagal.
    private static async Task<UserProfile> FetchGraphProfileAsync(IHttpClientFactory httpClientFactory, string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return new UserProfile(null, null);
        }

        var graph = httpClientFactory.CreateClient(GraphClientName);
        graph.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        string? displayName = null;
        try
        {
            var me = await graph.GetFromJsonAsync<GraphMe>("me");
            displayName = me?.DisplayName ?? me?.UserPrincipalName;
        }// Profil bersifat opsional, jadi login tetap dilanjutkan jika Microsoft Graph gagal diakses atau responsnya tidak sesuai.
        catch (HttpRequestException)
        {
            // profil opsional.
        }
        catch (JsonException)
        {
            // bentuk respons tidak sesuai.
        }

        string? photo = null;
        try
        {
            var bytes = await graph.GetByteArrayAsync("me/photo/$value");
            photo = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        }
        catch (HttpRequestException)
        {
            // sebagian akun tidak punya foto.
        }

        return new UserProfile(displayName, photo);
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

        // Gateway menerima /auth/v1/login lalu meneruskan ke Auth host sebagai /v1/login.
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

    private static IResult EntraChallenge(EntraBffOptions entra)
    {
        if (!entra.Enabled)
        {
            return Results.Redirect("/login?error=entra_disabled");
        }

        return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [EntraScheme]);
    }

    private static async Task<IResult> LogoutAsync(
        ITokenStore tokenStore,
        IUserProfileStore profileStore,
        HttpContext httpContext)
    {
        var sessionId = httpContext.User.FindFirst(BffClaims.SessionId)?.Value;
        if (sessionId is not null)
        {
            tokenStore.Remove(sessionId);
            profileStore.Remove(sessionId);
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }
}

// Kredensial login lokal dari form WebUI ke BFF. Token tak pernah dikirim balik ke browser.
public sealed record LoginRequest(string Username, string Password);

// Respons token dari gateway (login lokal & entra) dibaca di server dan disimpan di token store.
internal sealed record GatewayTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);

// Subset profil Microsoft Graph /me.
internal sealed record GraphMe(string? DisplayName, string? UserPrincipalName);

internal static class BffClaims
{
    public const string SessionId = "wms.bff.session";
}
