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

builder.Services.AddAuthentication()
    .AddOpenIdConnect("BOB.ID", "BOB.ID", options =>
    {
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.Authority = builder.Configuration["Oidc:Authority"];
        options.ClientId = builder.Configuration["Oidc:ClientId"];
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Scope.Add("roles");
    });

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<DomikManager>();
builder.Services.AddScoped<ResourceManager>();
builder.Services.AddSingleton<ICalculator, Calculator>();
builder.Services.AddScoped<CalculatorTick>();
builder.Services.AddHostedService<CalculatorBackgroundService>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByNameAsync("demo") == null)
    {
        var demo = new ApplicationUser { UserName = "demo", Email = "demo@domiki.local", EmailConfirmed = true };
        demo.PasswordHash = userManager.PasswordHasher.HashPassword(demo, "demo");
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

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (context.User.Identity?.Name == "demo"
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
    var result = await signInManager.PasswordSignInAsync("demo", "demo", isPersistent: false, lockoutOnFailure: false);
    return result.Succeeded
        ? Results.Ok(new { isAuthenticated = true, name = "demo" })
        : Results.Unauthorized();
});

app.MapFallbackToFile("index.html");

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<UnitOfWorkMiddleware>();
app.Run();
