using System.Threading.RateLimiting;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Sabro.API.Configuration;
using Sabro.API.Health;
using Sabro.API.Logto;
using Sabro.API.Media;
using Sabro.Biblical.Public;
using Sabro.Historical.Public;
using Sabro.Identity.Domain;
using Sabro.Identity.Public;
using Sabro.Lexicon.Public;
using Sabro.Play.Public;
using Sabro.Reviews.Public;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Localization;
using Sabro.Translations.Public;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName());

    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddOpenApi("v1", options =>
    {
        // Force enum schemas to be emitted as JSON strings with their member
        // names, matching the runtime JsonStringEnumConverter configured on the
        // MVC pipeline. Without this Microsoft.AspNetCore.OpenApi defaults to
        // the enum's underlying numeric type.
        options.AddSchemaTransformer<StringEnumSchemaTransformer>();
    });

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var authority = builder.Configuration["Logto:Authority"]
                ?? throw new InvalidOperationException("Logto:Authority is not configured.");
            var audience = builder.Configuration["Logto:Audience"]
                ?? throw new InvalidOperationException("Logto:Audience is not configured.");

            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthPolicies.Read, policy => policy.RequireAssertion(c => AuthPolicies.HasScope(c, AuthPolicies.Read)));
        options.AddPolicy(AuthPolicies.Write, policy => policy.RequireAssertion(c => AuthPolicies.HasScope(c, AuthPolicies.Write)));
        options.AddPolicy(AuthPolicies.Admin, policy => policy.RequireAssertion(c => AuthPolicies.HasScope(c, AuthPolicies.Admin)));

        // Area policies: role only. The admin scope is already required by the
        // class-level policy on every admin controller, and ASP.NET demands that
        // every applicable policy succeed, so these narrow rather than replace it.
        options.AddPolicy(AuthPolicies.LexiconView, policy => policy.Requirements.Add(
            new RolePermissionRequirement(
                p => RolePermissions.CanViewBackoffice(p, ContentArea.Lexicon), "view the Lexicon backoffice")));
        options.AddPolicy(AuthPolicies.LexiconEdit, policy => policy.Requirements.Add(
            new RolePermissionRequirement(
                p => RolePermissions.CanEdit(p, ContentArea.Lexicon), "edit the Lexicon")));
        options.AddPolicy(AuthPolicies.FiguresView, policy => policy.Requirements.Add(
            new RolePermissionRequirement(
                p => RolePermissions.CanViewBackoffice(p, ContentArea.Shmo), "view the figures backoffice")));
        options.AddPolicy(AuthPolicies.FiguresEdit, policy => policy.Requirements.Add(
            new RolePermissionRequirement(
                p => RolePermissions.CanEdit(p, ContentArea.Shmo), "edit the figures roster")));
        options.AddPolicy(AuthPolicies.ChantsView, policy => policy.Requirements.Add(
            new RolePermissionRequirement(
                p => RolePermissions.CanViewBackoffice(p, ContentArea.Nahlo), "view the chants backoffice")));
        options.AddPolicy(AuthPolicies.ChantsEdit, policy => policy.Requirements.Add(
            new RolePermissionRequirement(
                p => RolePermissions.CanEdit(p, ContentArea.Nahlo), "edit the Beth Gazo")));
    });

    builder.Services.AddScoped<IAuthorizationHandler, RolePermissionHandler>();

    // CORS so browser clients (the Sabro hub frontend, Meltho, future apps) on
    // other origins can call the API directly — the ecosystem's intended shape
    // (clients call /api/v1 directly). Origins come from config in production;
    // in Development we default to the local frontend ports so a fresh checkout
    // works without extra config. Bearer-token auth means we do not need
    // AllowCredentials (no cookies are sent to the API).
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if ((corsOrigins is null || corsOrigins.Length == 0) && builder.Environment.IsDevelopment())
    {
        corsOrigins = ["http://localhost:3000", "http://localhost:3100", "http://localhost:3200", "http://localhost:3300"];
    }

    corsOrigins ??= [];

    builder.Services.AddCors(options =>
        options.AddPolicy("frontend", policy => policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));
    });

    // /health reports on the database, not just the process — see HealthEndpoints.
    builder.Services.AddHealthChecks()
        .AddCheck<PostgresHealthCheck>(PostgresHealthCheck.Name);

    builder.Services.AddSabroSearch(builder.Configuration);

    // Logto Management API client — used by account deletion to erase the
    // caller's identity. Inert until Logto:ManagementApi credentials are set.
    builder.Services.Configure<LogtoManagementOptions>(
        builder.Configuration.GetSection(LogtoManagementOptions.SectionName));
    builder.Services.AddHttpClient<ILogtoManagementClient, LogtoManagementClient>();

    // Shared across modules (Lexicon publish gate, profile preferred-language) so
    // adding a language is one config change here, not a hardcoded list per module.
    builder.Services.Configure<SupportedLanguagesOptions>(
        builder.Configuration.GetSection(SupportedLanguagesOptions.SectionName));

    // Declared in SabroModules so the migration-coverage test reads the same list
    // this does, rather than a copy that can quietly disagree with it.
    var modules = SabroModules.All;

    foreach (var module in modules)
    {
        module.RegisterServices(builder.Services, builder.Configuration);
    }

    builder.Services.AddValidatorsFromAssemblies(modules
        .Select(m => m.GetType().Assembly)
        .Distinct());

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();

    // BEFORE UseStaticFiles, deliberately. CORS only decorates responses produced by
    // middleware that runs after it, so with static files first, /media/* answered with
    // no Access-Control-Allow-Origin. An <audio> element does not care — it loads
    // cross-origin without CORS — so playback worked and hid this. But fetch() does
    // care, and the word pages read the recording with fetch() to draw its waveform.
    // The symptom was a flat strip on every word, with nothing failing anywhere.
    app.UseCors("frontend");

    // Serves wwwroot/media (bibliography images, pronunciation recordings) — no auth,
    // matching "clients read content through validated URLs" for static assets.
    // The custom provider corrects the framework's IIS-derived defaults, which label
    // .ogg and .webm as video/* — an <audio> element may refuse those.
    app.UseStaticFiles(new StaticFileOptions
    {
        ContentTypeProvider = PronunciationAudioFormats.CreateContentTypeProvider(),
    });
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // /health = readiness (touches Postgres), /health/live = liveness (checks nothing).
    // The dependency check lives on /health deliberately: that is the URL UptimeRobot
    // already watches, and on 2026-07-31 it answered 200 through a total data outage.
    app.MapSabroHealthChecks();

    // Deployed build identity. BUILD_SHA is baked into the image by CD; the
    // post-deploy step asserts this endpoint carries the commit it just
    // shipped, so a stale container can never pass a deploy silently.
    var buildSha = app.Configuration["BUILD_SHA"] ?? "unknown";
    app.MapGet("/version", () => Results.Ok(new { sha = buildSha }));

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Sabro API terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
