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
using Rezervise.Application.Common.Serialization;
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

// Graylog GELF/UDP loglaması: konsol log timing korunur, yapılandırma hatası uygulamayı durdurmaz.
builder.Logging.AddGraylogLogging(builder.Configuration);

// Metrik hatti: OpenTelemetry toplar, Prometheus /metrics ucundan kazar.
// Yapılandırma hatası uygulamayı durdurmaz; metrikler sessizce devre dışı kalır.
builder.Services.AddRezerviseMetrics(builder.Configuration);

// Servisleri ekle
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Tek solution öldüğü için MediatR, Application assembly'sindeki Admin/Owner
// (backoffice) handler'larını da tarar; DI doğrulaması bunların servislerini de
// ister. Müşteri API'si bu handler'lari HTTP olarak expose ETMEZ; yalnızca
// taramanın geçerli olması için kayıtlar karşılanır.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Rezervise.Application.Common.Interfaces.IAdminAuditContext, Rezervise.Api.Services.AdminAuditContextAccessor>();

// Ortak altyapı: e-posta + SMS + push + denetim + özellik erişimi tek yerden.
builder.Services.AddSharedInfrastructure(
    builder.Configuration,
    builder.Environment.IsDevelopment());

// Gönderim servisleri arka planda yürür; kayıt akışı sağlayıcı beklemez.
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddHostedService<PushBackgroundService>();

// Zaman tabanlı randevu akışları: hatırlatıcı, otomatik tamamlama ve no-show.
builder.Services.AddHostedService<AppointmentWorkflowBackgroundService>();

// Telefon doğrulama (OTP) hatti: sağlayıcı bağlanana kadar log tabanlı SMS taşıması.
builder.Services.AddSmsInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

// Lokalizasyon: tr/en katalogları başlangıçta bellege yüklenir (runtime izleme yok)
builder.Services.AddSingleton<ILocalizationService, JsonLocalizationService>();

// Result<T> zarflarındaki anahtarlar tek noktada lokalize edilir.
builder.Services.AddScoped<LocalizedResultFilter>();

builder.Services.AddRequestLocalization(options =>
{
    var supportedCultures = new[] { "tr", "en" };

    options.DefaultRequestCulture = new RequestCulture("tr");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.FallBackToParentCultures = true;
    options.FallBackToParentUICultures = true;

    // Öncelik: 1) kullanıcı tercihi (User.LanguageCode / "language" claim)
    //          2) Accept-Language başlığı
    //          3) varsayılan "tr"
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

// CORS: açık izin listesi dışında wildcard kullanılmaz; wildcard origin ile
// credential birlikte asla açılmaz.
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
            // Yalnızca geliştirme/test ortamı için: credential olmadan wildcard.
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        // Üretimde boş liste = hiçbir origin'e izin yok (wildcard yok).
        policy.WithOrigins(origins)
              .WithHeaders(corsSettings.AllowedHeaders.ToArray())
              .WithMethods(corsSettings.AllowedMethods.ToArray());
    });
});

// Sağlık kontrolü: ek paket gerektirmeyen basit self-check ucu.
builder.Services.AddHealthChecks();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<LocalizedResultFilter>();
})
.AddJsonOptions(options =>
{
    // Bound the deşerialized graph depth to protect against deeply nested payloads.
    options.JsonSerializerOptions.MaxDepth = 32;
    // Tüm tarih alanları UTC "Z" ekiyle serileştirilir: mobil taraf saat dilimini
    // kaybedip yanlış yerel saat göstermesin (randevu, slot, personel vb. hepsi).
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Rezervise API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Örnek: 'Bearer {token}'",
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

// Ters proxy arkasında istemci IP'si ve şeması doğru okunur.
// Varsayılan olarak yalnızca loopback proxy'lere güvenilir; dağıtımda KnownProxies/KnownNetworks eklenmelidir.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Kimlik doğrulama önce çalışır: kullanıcı bazlı kültür sağlayıcısı
// HttpContext.User içindeki dil claim'ini okuyabilmelidir.
app.UseAuthentication();

// Kültür, hata üretiminden önce belirlenmiş olmalıdır.
app.UseRequestLocalization();

// Korelasyon kimliği hata middleware'inden önce üretilir ki loglar ve yanıtlar eşleşebilsin.
app.UseMiddleware<CorrelationIdMiddleware>();

// İstek/yanıt loglaması hata middleware'inin dışında olmalı: böylece üretilen
// 4xx/5xx yanıtları da response loğunda görünür.
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Güvenlik başlıkları HTTPS yönlendirmeleri dahil tüm yanıtlara eklenir.
app.UseMiddleware<SecurityHeadersMiddleware>();

// HSTS yalnızca HTTPS üzerinden servis edilen ortamlarda devrede.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// CORS, yetkilendirmeden önce çalışmalıdır.
app.UseCors(CorsSettings.PolicyName);

// Hiz sınırı yetkilendirmeden önce uygulanır: kimlik henüz çözülmemişse IP bazlı bölümleme kullanılır.
app.UseRateLimiter();

app.UseAuthorization();
app.MapControllers();

// Sağlık ucu: kimlik doğrulama gerektirmez, yalnızca process'in ayakta öldüğünü gösterir.
app.MapHealthChecks("/health").AllowAnonymous();

// Prometheus kazıma ucu. Canlı ortamda ağ seviyesinde kısıtlanmalıdır (reverse proxy / firewall).
var metricsSettings = builder.Configuration.GetSection(MetricsSettings.SectionName).Get<MetricsSettings>()
    ?? new MetricsSettings();

if (metricsSettings.Enabled)
{
    app.MapPrometheusScrapingEndpoint(metricsSettings.EndpointPath).AllowAnonymous();
}

// Veritabanını otomatik migrate et ve seed data ekle
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await dbContext.Database.MigrateAsync();

    // Seed sırasında üretilen domain event'leri e-posta/audit yan etkisi oluşturmaz.
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