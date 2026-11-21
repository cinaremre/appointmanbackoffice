using AppointMan.Application.Common.Interfaces;
using AppointMan.BackOffice.Api.Configuration;
using AppointMan.BackOffice.Api.Extensions;
using AppointMan.BackOffice.Api.Middleware;
using AppointMan.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AppointMan.Application;

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
            RoleClaimType = "role"
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

app.Run();
