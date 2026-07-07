using Domiki.Web.Business.Core;
using Domiki.Web.Data;
using Domiki.Models;
using Domiki.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Domiki.Web.Business;
using NLog.Web;
using NLog;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Security.Claims;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Host.UseNLog();

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Domiki"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

var authenticationBuilder = builder.Services.AddAuthentication();

var oidcAuthority = builder.Configuration["Oidc:Authority"];
if (!string.IsNullOrWhiteSpace(oidcAuthority))
{
    var oidcScheme = builder.Configuration["Oidc:Scheme"];
    if (string.IsNullOrWhiteSpace(oidcScheme)) oidcScheme = "oidc";
    var oidcDisplayName = builder.Configuration["Oidc:DisplayName"];
    if (string.IsNullOrWhiteSpace(oidcDisplayName)) oidcDisplayName = oidcScheme;
    authenticationBuilder.AddOpenIdConnect(oidcScheme, oidcDisplayName, options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.Authority = oidcAuthority;
        options.ClientId = builder.Configuration["Oidc:ClientId"];
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.CallbackPath = builder.Configuration["Oidc:CallbackPath"] ?? "/signin-oidc";
        options.SignedOutCallbackPath = builder.Configuration["Oidc:SignedOutCallbackPath"] ?? "/signout-callback-oidc";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Scope.Add("roles");
        options.Events.OnRemoteFailure = context =>
        {
            logger.Warn(context.Failure, "OIDC remote failure on {0}: {1}", oidcScheme, context.Failure?.Message);

            context.HandleResponse();
            context.Response.Redirect($"/Identity/Account/Login?remoteError={Uri.EscapeDataString(context.Failure?.Message ?? "External sign-in failed.")}");
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<DomikManager>();
builder.Services.AddScoped<OrderManager>();
builder.Services.AddScoped<ResourceManager>();
builder.Services.AddScoped<PlayerResourceManager>();
builder.Services.AddScoped<WorkerManager>();
builder.Services.AddScoped<WeatherManager>();
builder.Services.AddScoped<BlueprintManager>();
builder.Services.AddScoped<ExpeditionManager>();
builder.Services.AddScoped<VillageLevelCalculator>();
builder.Services.AddSingleton<ICalculator, Calculator>();
builder.Services.AddScoped<CalculatorTick>();
builder.Services.AddHostedService<CalculatorBackgroundService>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/wasm",
        "image/svg+xml",
        "application/manifest+json",
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

builder.Services.AddHealthChecks();

var app = builder.Build();

var demoUserName = app.Configuration["Demo:UserName"];
var demoEmail = app.Configuration["Demo:Email"];
var demoPassword = app.Configuration["Demo:Password"];

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByNameAsync(demoUserName) == null)
    {
        var demo = new ApplicationUser { UserName = demoUserName, Email = demoEmail, EmailConfirmed = true };
        demo.PasswordHash = userManager.PasswordHasher.HashPassword(demo, demoPassword);
        await userManager.CreateAsync(demo);
    }
}

var forwardedHeadersOptions = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto };
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-Frame-Options"] = "DENY";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.OnStarting(() =>
    {
        if (context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true
            && !headers.ContainsKey("Cache-Control"))
        {
            headers.CacheControl = "no-cache";
        }
        return Task.CompletedTask;
    });
    await next();
});

app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path.Value;
        if (path != null && path.StartsWith("/assets/", StringComparison.Ordinal))
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    }
});
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (context.User.Identity?.Name == demoUserName
        && (path.StartsWithSegments("/Identity/Account/Manage")
            || path.StartsWithSegments("/Identity/Account/ExternalLogin")))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    await next();
});

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");
app.MapRazorPages();

app.MapHealthChecks("/healthz");

app.MapGet("/authentication/login", (string returnUrl) =>
{
    var target = !string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
        ? returnUrl
        : "/";
    return Results.Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(target)}");
});

app.MapGet("/authentication/logout", () => Results.SignOut(
    new AuthenticationProperties { RedirectUri = "/" },
    new[] { IdentityConstants.ApplicationScheme }));

app.MapGet("/authentication/user", (HttpContext http) =>
{
    var user = http.User;
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Ok(new { isAuthenticated = false });
    }

    var name = user.FindFirstValue("name")
               ?? user.FindFirstValue("preferred_username")
               ?? user.FindFirstValue("email")
               ?? user.Identity?.Name;
    return Results.Ok(new { isAuthenticated = true, name });
});

app.MapPost("/authentication/demo", async (HttpContext http, SignInManager<ApplicationUser> signInManager) =>
{
    if (http.User.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new { isAuthenticated = true, name = http.User.Identity.Name });
    }
    var result = await signInManager.PasswordSignInAsync(demoUserName, demoPassword, isPersistent: false, lockoutOnFailure: false);
    return result.Succeeded
        ? Results.Ok(new { isAuthenticated = true, name = demoUserName })
        : Results.Unauthorized();
});

app.MapFallbackToFile("index.html");

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<UnitOfWorkMiddleware>();
app.Run();
