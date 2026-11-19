using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Middleware;
using RaceDay.Api.Services;
using RaceDay.Api.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "Call POST /api/auth/login first (e.g. one of the demo accounts in the README, " +
            "password RaceDay@2026) - Swagger UI keeps the .RaceDay.Session cookie it gets back " +
            "and sends it automatically on every \"Try it out\" call after that, so protected " +
            "endpoints just work without any extra setup.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEnrolmentService, EnrolmentService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();

// Blob storage: registered only when a connection string is actually
// present, so a fresh clone or the test suite (neither of which has an
// Azure account) still starts and runs normally - only an actual upload
// attempt then fails, with a clear 503, via NotConfiguredBlobStorageService.
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection(BlobStorageOptions.SectionName));
var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage");
if (!string.IsNullOrWhiteSpace(blobConnectionString))
{
    builder.Services.AddSingleton<IBlobStorageService>(sp =>
        new AzureBlobStorageService(blobConnectionString, sp.GetRequiredService<IOptions<BlobStorageOptions>>()));
}
else
{
    builder.Services.AddSingleton<IBlobStorageService, NotConfiguredBlobStorageService>();
}

// Server-side session: login writes UserId/Role in here, and
// SessionAuthenticationHandler reads them back out to build the
// ClaimsPrincipal for [Authorize]. The session store itself (not a JWT)
// is what the brief and rubric call for.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services
    .AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, options => { });
builder.Services.AddAuthorization();

// Without this, the session cookie's protection keys live only in the
// container's memory - a restart (or a rolling redeploy) silently
// invalidates every logged-in user's session. DataProtection:KeysPath is
// unset locally (the default in-memory/registry key ring is fine for one
// dev machine) and set to the mounted /keys volume in docker-compose.yml.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

// Swagger is independent of seeding/migration below: on by default in
// Development (unchanged Visual Studio behaviour), and separately
// switchable with Swagger:Enabled=true for Docker (docker-compose sets it),
// where the environment is Production but the student still wants the UI.
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Database startup, container-friendly. Both flags default to false/unset,
// so existing Visual Studio usage (dotnet ef database update, then F5) is
// completely unaffected - this only activates when Docker's compose file
// sets Database__MigrateOnStartup/SeedDemoData explicitly, or Development
// (which already seeded unconditionally before this change, so that
// existing behaviour is preserved rather than narrowed).
var migrateOnStartup = builder.Configuration.GetValue<bool>("Database:MigrateOnStartup");
var seedDemoData = builder.Configuration.GetValue<bool>("Database:SeedDemoData") || app.Environment.IsDevelopment();

if (migrateOnStartup)
{
    using var migrationScope = app.Services.CreateScope();
    var migrationDb = migrationScope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

    // The SQL Server container can take a while to start accepting
    // connections even once its own healthcheck passes (compose's
    // depends_on: condition: service_healthy reduces, but doesn't
    // eliminate, this race) - retry rather than fail the whole app on the
    // first attempt.
    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await migrationDb.Database.MigrateAsync();
            break;
        }
        catch (Exception) when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

if (seedDemoData)
{
    // Seeds demo data on first run only (skipped once Users has any rows).
    using var seedScope = app.Services.CreateScope();
    var seedDb = seedScope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    await DbSeeder.SeedAsync(seedDb);
}

// Compose's services talk plain HTTP to each other on the internal Docker
// network - there's no certificate inside the container to redirect to,
// and nothing outside the network needs this app's own TLS since the
// compose file doesn't expose 443 at all.
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

// Exposes Program to WebApplicationFactory<Program> in the test project.
public partial class Program { }
