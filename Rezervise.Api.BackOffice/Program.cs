using Rezervise.Application;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Serialization;
using Rezervise.BackOffice.Api.Configuration;
using Rezervise.BackOffice.Api.Extensions;
using Rezervise.BackOffice.Api.Middleware;
using Rezervise.Infrastructure;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Rezervise.Application;
using Rezervise.Infrastructure;
using Rezervise.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ===== Yapılandırma =====
var jwtSettings = builder.Configuration.GetSection(BackOfficeJwtSettings.SectionName)
    .Get<BackOfficeJwtSettings>() ?? new BackOfficeJwtSettings();

var paginationSettings = builder.Configuration.GetSection(PaginationSettings.SectionName)
    .Get<PaginationSettings>() ?? new PaginationSettings();

builder.Services.AddSingleton(paginationSettings);

// ===== Katmanlar =====
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// MediatR tüm Application assembly'sını tarar: müşteri handler'larının
// bağımlılıkleri (e-posta kuyruğu, OTP, push, lokalizasyon) DI doğrulaması için
// burada da karşılanır; müşteri HTTP uçları backoffice'te açık DEGILDIR.
// Ortak altyapı tek uzantı ile kurulur (unutmaya karşı).
builder.Services.AddSharedInfrastructure(
    builder.Configuration,
    builder.Environment.IsDevelopment());
builder.Services.AddHostedService<Rezervise.Api.BackOffice.Services.PushBackgroundService>();
builder.Services.AddSingleton<Rezervise.Application.Common.Interfaces.ILocalizationService, BackOfficeLocalizationService>();

// ===== Yönetici kimliği: claim'lerden çözelir, gövdeye güvenilmez =====
builder.Services.AddBackOfficeCurrentUser();

// ===== Kimlik doğrulama: ayrı audience, müşteri token'lari geçersiz =====
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = jwtSettings.RequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = jwtSettings.ClockSkew,
            // TokenService rolleri ClaimTypes.Role olarak yazar; [Authorize(Roles=...)]
            // doğrulaması aynı claim türünü okumalıdır.
            RoleClaimType = ClaimTypes.Role
        };
    });

// ===== Permission bazlı yetki =====
builder.Services.AddBackOfficeAuthorization();

// ===== Denetim bağlamı =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAdminAuditContext, AdminAuditContextAccessor>();

// ===== MVC + ProblemDetails =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Tüm tarih alanları UTC "Z" ekiyle serileştirilir (mobil API ile aynı kural).
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Model doğrulama hataları RFC 9457 ProblemDetails ile döner.
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://api.rezervise.com/problems/validation",
            Title = "İstek doğrulaması başarısız.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Gönderilen alanlar kurallara uygun değil.",
            Extensions =
            {
                ["traceId"] = context.HttpContext.Response.Headers["X-Correlation-ID"].ToString(),
                ["errors"] = context.ModelState
                    .Where(kv => kv.Value?.Errors.Count > 0)
                    .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
            }
        };

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

// Health endpoint: orkestrasyon ve izleme için (doküman 20.2). DB erişilemezse
// 503 döner ve istemciye sabit mesaj verilir.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ===== Rate limiting: backoffice ayrı pencere =====
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("BackOfficeGlobal", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        partition => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1)
        }));
});

// ===== CORS: backoffice origin allowlist =====
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("BackOffice", policy =>
    {
        if (allowedOrigins.Length == 0)
            policy.SetIsOriginAllowed(_ => false);
        else
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();

        policy.AllowCredentials();
    });
});

// ===== OpenAPI: yalnızca Development ortamında açık (doküman 24) =====
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// ===== Pipeline =====
app.UseMiddleware<BackOfficeExceptionMiddleware>();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("BackOffice");

app.UseAuthentication();
app.UseAuthorization();

// Health endpoint: orkestrasyon ve izleme için (doküman 20.2). DB erişilemezse
// 503 döner ve istemciye istemcinin doğrudan gösterebileceği mesaj taşınır.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = static async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        if (report.Status == HealthStatus.Healthy)
        {
            await context.Response.WriteAsync("""{"status":"healthy"}""", context.RequestAborted);
            return;
        }

        await context.Response.WriteAsync(
            """{"statusCode":503,"code":"service_unavailable","message":"İşlem Şu Anda Gerçekleştirilemiyor. Lütfen Birazdan Tekrar Deneyin."}""",
            context.RequestAborted);
    }
})
.AllowAnonymous();

app.MapControllers();

// Veritabanını otomatik migrate et. Docker/Sunucu kurulumlarında tek
// hoşt kalkınca örnek veri de hazırlansın diye migrate sonrası seed
// çalıştırılır (müşteri API'si ile aynı desen: yan etkiler kapatılır).
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Rezervise.Infrastructure.Persistence.Context.AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await dbContext.Database.MigrateAsync();

    // Seed sırasında üretilen domain event'leri e-posta/audit yan etkisi üretmez.
    var sideEffects = scope.ServiceProvider.GetRequiredService<Rezervise.Application.Common.Services.EventSideEffectDeduplicator>();
    var sideEffectsWereEnabled = sideEffects.Enabled;
    sideEffects.Enabled = false;

    try
    {
        await Rezervise.Infrastructure.Persistence.SeedData.SeedOrchestrator.SeedAllAsync(dbContext, logger);
    }
    finally
    {
        sideEffects.Enabled = sideEffectsWereEnabled;
    }
}

app.Run();
