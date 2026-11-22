using Rezervise.Application.Common.Interfaces;
using Rezervise.BackOffice.Api.Configuration;
using Rezervise.BackOffice.Api.Extensions;
using Rezervise.BackOffice.Api.Middleware;
using Rezervise.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Rezervise.Application;
using Rezervise.Infrastructure;
using Rezervise.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ===== Yapilandirma =====
var jwtSettings = builder.Configuration.GetSection(BackOfficeJwtSettings.SectionName)
    .Get<BackOfficeJwtSettings>() ?? new BackOfficeJwtSettings();

var paginationSettings = builder.Configuration.GetSection(PaginationSettings.SectionName)
    .Get<PaginationSettings>() ?? new PaginationSettings();

builder.Services.AddSingleton(paginationSettings);

// ===== Katmanlar =====
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// MediatR tum Application assembly'sini tarar: musteri handler'larinin
// bagimlilikleri (e-posta kuyrugu, OTP, push, lokalizasyon) DI dogrulamasi icin
// burada da karsilanir; musteri HTTP uclari backoffice'te acik DEGILDIR.
builder.Services.AddEmailInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddSmsInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddPushInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Rezervise.Api.BackOffice.Services.PushBackgroundService>();
builder.Services.AddSingleton<Rezervise.Application.Common.Interfaces.ILocalizationService, BackOfficeLocalizationService>();

// ===== Yonetici kimligi: claim'lerden cozelir, govdeye guvenilmez =====
builder.Services.AddBackOfficeCurrentUser();

// ===== Kimlik dogrulama: ayri audience, musteri token'lari gecersiz =====
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
            // dogrulamasi ayni claim turunu okumalidir.
            RoleClaimType = ClaimTypes.Role
        };
    });

// ===== Permission bazli yetki =====
builder.Services.AddBackOfficeAuthorization();

// ===== Denetim baglami =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAdminAuditContext, AdminAuditContextAccessor>();

// ===== MVC + ProblemDetails =====
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Model dogrulama hatalari RFC 9457 ProblemDetails ile doner.
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://api.rezervise.com/problems/validation",
            Title = "Istek dogrulamasi basarisiz.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Gonderilen alanlar kurallara uygun degil.",
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

// ===== Rate limiting: backoffice ayri pencere =====
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

// ===== OpenAPI: yalnizca Development ortaminda acik (dokuman 24) =====
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

// Health endpoint: orkestrasyon ve izleme icin (dokuman 20.2).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();

app.MapControllers();

// Veritabanini otomatik migrate et. Docker/Sunucu kurulumlarinda tek
// host kalkinca ornek veri de hazirlansin diye migrate sonrasi seed
// calistirilir (musteri API'si ile ayni desen: yan etkiler kapatilir).
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Rezervise.Infrastructure.Persistence.Context.AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await dbContext.Database.MigrateAsync();

    // Seed sirasinda uretilen domain event'leri e-posta/audit yan etkisi uretmez.
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
