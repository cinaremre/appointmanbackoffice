using AppointMan.Api.Localization;
using AppointMan.Api.Middleware;
using AppointMan.Application;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Infrastructure;
using AppointMan.Infrastructure.Persistence.Context;
using AppointMan.Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<LocalizedResultFilter>();
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

// Kimlik dogrulama once calisir: kullanici bazli kultur saglayicisi
// HttpContext.User icindeki dil claim'ini okuyabilmelidir.
app.UseAuthentication();

// Kultur, hata uretiminden once belirlenmis olmalidir.
app.UseRequestLocalization();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

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