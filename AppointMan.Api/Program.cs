using AppointMan.Api.Authorization;
using AppointMan.Api.Configuration;
using AppointMan.Api.Localization;
using AppointMan.Api.Logging;
using AppointMan.Api.Metrics;
using AppointMan.Api.Middleware;
using AppointMan.Api.RateLimiting;
using AppointMan.Api.Services;
using AppointMan.Application;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Infrastructure;
using AppointMan.Infrastructure.Persistence.Context;
using AppointMan.Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Security guards: the API must not start with missing, weak or unsafe configuration.
builder.Configuration.ValidateAllowedHosts(builder.Environment);
builder.Configuration.GetRequiredConnectionString();
builder.Services.AddValidatedJwtSettings(builder.Configuration);

// Graylog GELF/UDP loglamasi: konsol log timing korunur, yapilandirma hatasi uygulamayi durdurmaz.
builder.Logging.AddGraylogLogging(builder.Configuration);

// Metrik hatti: OpenTelemetry toplar, Prometheus /metrics ucundan kazar.
// Yapilandirma hatasi uygulamayi durdurmaz; metrikler sessizce devre disi kalir.
builder.Services.AddAppointManMetrics(builder.Configuration);

// Servisleri ekle
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Lokalizasyon: tr/en kataloglari baslangicta bellege yuklenir (runtime izleme yok)
builder.Services.AddSingleton<ILocalizationService, JsonLocalizationService>();

// Result<T> zarflarindaki anahtarlar tek noktada lokalize edilir.
builder.Services.AddScoped<LocalizedResultFilter>();

builder.Services.AddRequestLocalization(options =>
{
    var supportedCultures = new[] { "tr", "en" };

    options.DefaultRequestCulture = new RequestCulture("tr");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.FallBackToParentCultures = true;
    options.FallBackToParentUICultures = true;

    // Oncelik: 1) kullanici tercihi (User.LanguageCode / "language" claim)
    //          2) Accept-Language basligi
    //          3) varsayilan "tr"
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new UserLanguageCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

// JWT Authentication
// Values come from the validated JwtSettings options; the secret is supplied by the environment.
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException($"Configuration section '{JwtSettings.SectionName}' is missing.");
var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = jwtSettings.RequireHttpsMetadata;
    // The API never reads the token back from the context; keeping a copy only widens the exposure surface.
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = jwtSettings.ClockSkew,
        // Only HS256 is accepted; prevents algorithm substitution attacks.
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
    };
});

// Ambient identity: handlers resolve the caller from the signed token, never from the request body.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(AuthorizationPolicies.BusinessOwnerOnly, policy => policy.RequireRole("BusinessOwner"));
    options.AddPolicy(AuthorizationPolicies.CustomerOnly, policy => policy.RequireRole("Customer"));
    options.AddPolicy(AuthorizationPolicies.AdministratorOnly, policy => policy.RequireRole("Admin", "SuperAdmin"));
});

builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<LocalizedResultFilter>();
})
.AddJsonOptions(options =>
{
    // Bound the deserialized graph depth to protect against deeply nested payloads.
    options.JsonSerializerOptions.MaxDepth = 32;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AppointMan API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ornek: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ters proxy arkasinda istemci IP'si ve semasi dogru okunur.
// Varsayilan olarak yalnizca loopback proxy'lere guvenilir; dagitimda KnownProxies/KnownNetworks eklenmelidir.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Kimlik dogrulama once calisir: kullanici bazli kultur saglayicisi
// HttpContext.User icindeki dil claim'ini okuyabilmelidir.
app.UseAuthentication();

// Kultur, hata uretiminden once belirlenmis olmalidir.
app.UseRequestLocalization();

// Korelasyon kimligi hata middleware'inden once uretilir ki loglar ve yanitlar eslesebilsin.
app.UseMiddleware<CorrelationIdMiddleware>();

// Istek/yanit loglamasi hata middleware'inin disinda olmali: boylece uretilen
// 4xx/5xx yanitlari da response logunda gorunur.
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// HSTS yalnizca HTTPS uzerinden servis edilen ortamlarda devrede.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Guvenlik basliklari tum yanitlara eklenir.
app.UseMiddleware<SecurityHeadersMiddleware>();

// CORS, yetkilendirmeden once calismalidir.
app.UseCors(CorsSettings.PolicyName);

// Hiz siniri yetkilendirmeden once uygulanir: kimlik henuz cozulmemisse IP bazli bolumleme kullanilir.
app.UseRateLimiter();

app.UseAuthorization();
app.MapControllers();

// Prometheus kazima ucu. Canli ortamda ag seviyesinde kisitlanmalidir (reverse proxy / firewall).
var metricsSettings = builder.Configuration.GetSection(MetricsSettings.SectionName).Get<MetricsSettings>()
    ?? new MetricsSettings();

if (metricsSettings.Enabled)
{
    app.MapPrometheusScrapingEndpoint(metricsSettings.EndpointPath).AllowAnonymous();
}

// Veritabanini otomatik migrate et ve seed data ekle
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await dbContext.Database.MigrateAsync();

    // Seed data
    await SeedOrchestrator.SeedAllAsync(dbContext, logger);
}

app.Run();