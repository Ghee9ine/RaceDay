using Microsoft.AspNetCore.DataProtection;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ApiUnauthorizedExceptionFilter>();
});

builder.Services.AddHttpContextAccessor();

// Without this, a container restart silently invalidates every logged-in
// user's .RaceDay.Web.Session cookie (the cookie value itself is Data
// Protection-wrapped). Unset locally; set to the mounted /keys volume in
// docker-compose.yml.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

// Session: this app's OWN session (.RaceDay.Web.Session), distinct from
// the API's (.RaceDay.Session). Holds UserId/Role/FullName/the API's own
// session cookie value - see SessionKeys and the README's "MVC architecture"
// section for why the two are kept clearly separate rather than sharing
// one name or one store.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddTransient<ApiSessionCookieHandler>();
builder.Services.AddScoped<SessionUser>();

// UseCookies = false is mandatory: pooled HttpClientHandlers are shared
// across every user of this app's process, so a shared CookieContainer
// would leak one user's .RaceDay.Session value into another user's
// requests. ApiSessionCookieHandler instead reads each request's own
// session fresh, every time, and attaches only that user's own cookie.
builder.Services.AddHttpClient<IRaceDayApiClient, RaceDayApiClient>(client =>
    {
        var baseUrl = builder.Configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
        client.BaseAddress = new Uri(baseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
    .AddHttpMessageHandler<ApiSessionCookieHandler>();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseStaticFiles();

app.UseSession();
app.UseRouting();

// Authenticated pages must never be served from the back/forward cache -
// logging out must actually hide a protected page from history, not just
// redirect away from it once.
//
// Set the header after the pipeline runs, not via Response.OnStarting:
// a request that WRITES to the session during its own handling (login,
// which calls Session.SetInt32 then redirects) throws
// "Session has not been configured for this application or request" if
// Session is read from inside an OnStarting callback for that same
// request - SessionMiddleware's own OnStarting-based commit appears to
// have already torn the feature down by the time a later-registered
// OnStarting callback fires for a response that was still being written
// to session this late. Reading it synchronously right after `next()`
// returns, before the response has started, avoids that entirely and
// still sees the just-set session values.
app.Use(async (context, next) =>
{
    await next();
    if (!context.Response.HasStarted && context.Session.GetInt32(SessionKeys.UserId) is not null)
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

await app.RunAsync();

public partial class Program
{
}
