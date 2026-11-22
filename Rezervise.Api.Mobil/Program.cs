using Rezervise.Api.Authorization;
using Rezervise.Api.Configuration;
using Rezervise.Api.Email;
using Rezervise.Api.Localization;
using Rezervise.Api.Logging;
using Rezervise.Api.Metrics;
using Rezervise.Api.Middleware;
using Rezervise.Api.Push;
using Rezervise.Api.RateLimiting;
using Rezervise.Api.Services;
using Rezervise.Api.Workflow;
using Rezervise.Application;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Services;
using Rezervise.Infrastructure;
using Rezervise.Infrastructure.Extensions;
using Rezervise.Infrastructure.Persistence.Context;
using Rezervise.Infrastructure.Persistence.SeedData;
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
builder.Services.AddRezerviseMetrics(builder.Configuration);

// Servisleri ekle
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Tek solution oldugu icin MediatR, Application assembly'sindeki Admin/Owner
// (backoffice) handler'larini da tarar; DI dogrulamasi bunlarin servislerini de
// ister. Musteri API'si bu handler'lari HTTP olarak expose ETMEZ; yalnizca
// taramanin gecerli olmasi icin kayitlar karsilanir.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Rezervise.Application.Common.Interfaces.IAdminAuditContext, Rezervise.Api.Services.AdminAuditContextAccessor>();

// Ortak altyapi: e-posta + SMS + push + denetim + ozellik erisimi tek yerden.
builder.Services.AddSharedInfrastructure(
    builder.Configuration,
    builder.Environment.IsDevelopment());

// Gonderim servisleri arka planda yurur; kayit akisi saglayici beklemez.
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddHostedService<PushBackgroundService>();

// Zaman tabanli randevu akislari: hatirlatici, otomatik tamamlama ve no-show.
builder.Services.AddHostedService<AppointmentWorkflowBackgroundService>();

// Telefon dogrulama (OTP) hatti: saglayici baglanana kadar log tabanli SMS tasimasi.
builder.Services.AddSmsInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

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

// CORS: acik izin listesi disinda wildcard kullanilmaz; wildcard origin ile
// credential birlikte asla acilmaz.
var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()
    ?? new CorsSettings();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsSettings.PolicyName, policy =>
    {
        var origins = corsSettings.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .ToArray();

        if (origins.Length == 0 && builder.Environment.IsDevelopment())
        {
            // Yalnizca gelistirme/test ortami icin: credential olmadan wildcard.
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        // Uretimde bos liste = hicbir origin'e izin yok (wildcard yok).
        policy.WithOrigins(origins)
              .WithHeaders(corsSettings.AllowedHeaders.ToArray())
              .WithMethods(corsSettings.AllowedMethods.ToArray());
    });
});

// Saglik kontrolu: ek paket gerektirmeyen basit self-check ucu.
builder.Services.AddHealthChecks();

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
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Rezervise API", Version = "v1" });

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

// Guvenlik basliklari HTTPS yonlendirmeleri dahil tum yanitlara eklenir.
app.UseMiddleware<SecurityHeadersMiddleware>();

// HSTS yalnizca HTTPS uzerinden servis edilen ortamlarda devrede.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// CORS, yetkilendirmeden once calismalidir.
app.UseCors(CorsSettings.PolicyName);

// Hiz siniri yetkilendirmeden once uygulanir: kimlik henuz cozulmemisse IP bazli bolumleme kullanilir.
app.UseRateLimiter();

app.UseAuthorization();
app.MapControllers();

// Saglik ucu: kimlik dogrulama gerektirmez, yalnizca process'in ayakta oldugunu gosterir.
app.MapHealthChecks("/health").AllowAnonymous();

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

    // Seed sirasinda �retilen domain event'leri e-posta/audit yan etkisi olusturmaz.
    var sideEffects = scope.ServiceProvider.GetRequiredService<EventSideEffectDeduplicator>();
    var sideEffectsWereEnabled = sideEffects.Enabled;
    sideEffects.Enabled = false;

    try
    {
        await SeedOrchestrator.SeedAllAsync(dbContext, logger);
    }
    finally
    {
        sideEffects.Enabled = sideEffectsWereEnabled;
    }
}

app.Run();