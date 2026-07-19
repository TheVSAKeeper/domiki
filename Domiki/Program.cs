using Domiki.Web.Activities;
using Domiki.Web.Core;
using Domiki.Web.Core.Scheduling;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Domiki.Web.Workers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using System.IO.Compression;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    });

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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
        if (string.IsNullOrWhiteSpace(oidcScheme))
        {
            oidcScheme = "oidc";
        }

        var oidcDisplayName = builder.Configuration["Oidc:DisplayName"];
        if (string.IsNullOrWhiteSpace(oidcDisplayName))
        {
            oidcDisplayName = oidcScheme;
        }

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
                Log.Warning(context.Failure, "OIDC remote failure on {Scheme}: {Message}", oidcScheme, context.Failure?.Message);

                context.HandleResponse();
                context.Response.Redirect($"/Identity/Account/Login?remoteError={Uri.EscapeDataString(context.Failure?.Message ?? "External sign-in failed.")}");
                return Task.CompletedTask;
            };
        });
    }

    builder.Services.AddAuthorization();

    builder.Services.AddControllersWithViews(options =>
    {
        options.OutputFormatters.RemoveType<StringOutputFormatter>();
        options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
    });

    builder.Services.AddRazorPages();
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
    builder.Services.AddScoped<UnitOfWork>();
    builder.Services.AddScoped<DomikManager>();
    builder.Services.AddScoped<OrderManager>();
    builder.Services.AddScoped<ErrandManager>();
    builder.Services.AddScoped<IncidentManager>();
    builder.Services.AddScoped<GiftManager>();
    builder.Services.AddSingleton<ResourceManager>();
    builder.Services.AddScoped<PlayerResourceManager>();
    builder.Services.AddScoped<WorkerManager>();
    builder.Services.AddScoped<WeatherManager>();
    builder.Services.AddScoped<BlueprintManager>();
    builder.Services.AddScoped<ExpeditionManager>();
    builder.Services.AddScoped<DecorManager>();
    builder.Services.AddScoped<TolokaManager>();
    builder.Services.AddScoped<MarketManager>();
    builder.Services.AddScoped<WorldManager>();
    builder.Services.AddScoped<SeasonManager>();
    builder.Services.AddScoped<VillageLevelCalculator>();
    builder.Services.AddScoped<GuestbookManager>();
    builder.Services.AddScoped<HelpManager>();
    builder.Services.AddScoped<PlayerEventManager>();
    builder.Services.AddScoped<GoalManager>();
    builder.Services.AddScoped<PushManager>();
    builder.Services.AddSingleton<PushSender>();
    builder.Services.AddSingleton<GameStateBroker>();
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

    builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    var demoUserName = app.Configuration["Demo:UserName"]!;
    var demoEmail = app.Configuration["Demo:Email"]!;
    var demoPassword = app.Configuration["Demo:Password"]!;

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

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseHsts();
    }

    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseResponseCompression();

    app.UseMiddleware<PrecompressedStaticFilesMiddleware>();

    var staticContentTypes = new FileExtensionContentTypeProvider();
    staticContentTypes.Mappings[".br"] = "application/octet-stream";
    staticContentTypes.Mappings[".gz"] = "application/octet-stream";

    app.UseStaticFiles(new StaticFileOptions
    {
        ContentTypeProvider = staticContentTypes,
        OnPrepareResponse = context =>
        {
            var path = context.Context.Request.Path.Value;
            if (path != null && path.StartsWith("/assets/", StringComparison.Ordinal))
            {
                context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            }
            else if (path == "/sw.js")
            {
                context.Context.Response.Headers.CacheControl = "no-cache";
            }
        },
    });

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<DemoAccountGuardMiddleware>();

    app.UseWhen(context => !context.Request.Path.StartsWithSegments("/Domiki/Stream"),
        branch => branch.UseMiddleware<UnitOfWorkMiddleware>());

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapControllers();
    app.MapControllerRoute("default",
        "{controller}/{action=Index}/{id?}");

    app.MapRazorPages();

    app.MapHealthChecks("/healthz");

    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Приложение аварийно завершилось при старте");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
