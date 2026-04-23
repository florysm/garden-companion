using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Features.Auth;
using GardenCompanion.Api.Features.AmendmentLogs;
using GardenCompanion.Api.Features.Households;
using GardenCompanion.Api.Features.UserInsights;
using GardenCompanion.Api.Features.Users;
using GardenCompanion.Api.Features.WeatherObservations;
using GardenCompanion.Api.Features.GardenBeds;
using GardenCompanion.Api.Features.GardenMembers;
using GardenCompanion.Api.Features.Gardens;
using GardenCompanion.Api.Features.GardenTasks;
using GardenCompanion.Api.Features.HarvestLogs;
using GardenCompanion.Api.Features.PestDiseaseLogs;
using GardenCompanion.Api.Features.PlantingObservations;
using GardenCompanion.Api.Features.Plantings;
using GardenCompanion.Api.Features.Plants;
using GardenCompanion.Api.Features.SoilTests;
using GardenCompanion.Api.Infrastructure.Data;
using GardenCompanion.Api.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── JSON ──────────────────────────────────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT ───────────────────────────────────────────────────────────────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"]
    ?? throw new InvalidOperationException("JWT Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names as they appear in the JWT (e.g. "sub") instead of
        // remapping to legacy WS-Federation URIs like ClaimTypes.NameIdentifier.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── MediatR ───────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Email ─────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                builder.Configuration["App:FrontendUrl"] ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Migrate on startup (dev convenience) ─────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    app.MapOpenApi();
}

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────────────
var api = app.MapGroup("/api");

// Auth (anonymous)
RegisterUserEndpoint.Map(api);
LoginUserEndpoint.Map(api);
RefreshTokenEndpoint.Map(api);
ForgotPasswordEndpoint.Map(api);
ResetPasswordEndpoint.Map(api);

// Users
GetMyProfileEndpoint.Map(api);
UpdateMyProfileEndpoint.Map(api);
GetMySettingsEndpoint.Map(api);
UpdateMySettingsEndpoint.Map(api);

// Households
GetHouseholdEndpoint.Map(api);
UpdateHouseholdEndpoint.Map(api);
AddHouseholdMemberEndpoint.Map(api);
RemoveHouseholdMemberEndpoint.Map(api);
GetWeatherStationEndpoint.Map(api);
UpsertWeatherStationEndpoint.Map(api);
DeleteWeatherStationEndpoint.Map(api);

// Gardens
GetGardenTypesEndpoint.Map(api);
GetGardensEndpoint.Map(api);
CreateGardenEndpoint.Map(api);
GetGardenEndpoint.Map(api);
UpdateGardenEndpoint.Map(api);
DeleteGardenEndpoint.Map(api);

// Garden Beds
CreateGardenBedEndpoint.Map(api);
GetGardenBedEndpoint.Map(api);
UpdateGardenBedEndpoint.Map(api);
DeleteGardenBedEndpoint.Map(api);

// Garden Members
AddGardenMemberEndpoint.Map(api);
RemoveGardenMemberEndpoint.Map(api);

// Garden Tasks
CreateGardenTaskEndpoint.Map(api);
GetGardenTasksEndpoint.Map(api);
GetGardenTaskEndpoint.Map(api);
UpdateGardenTaskEndpoint.Map(api);
CompleteGardenTaskEndpoint.Map(api);
DeleteGardenTaskEndpoint.Map(api);

// Soil Tests
CreateSoilTestEndpoint.Map(api);
GetSoilTestsEndpoint.Map(api);
GetSoilTestEndpoint.Map(api);

// Plants
SearchPlantsEndpoint.Map(api);
GetPlantEndpoint.Map(api);
CreatePlantEndpoint.Map(api);
GetPlantCompanionsEndpoint.Map(api);
AddPlantCompanionEndpoint.Map(api);
RemovePlantCompanionEndpoint.Map(api);

// Plantings
CreatePlantingEndpoint.Map(api);
GetPlantingsEndpoint.Map(api);
GetPlantingEndpoint.Map(api);
UpdatePlantingEndpoint.Map(api);
UpdatePlantingStatusEndpoint.Map(api);
DeletePlantingEndpoint.Map(api);

// Planting Observations
AddPlantingObservationEndpoint.Map(api);
GetPlantingObservationsEndpoint.Map(api);

// Harvest Logs
LogHarvestEndpoint.Map(api);
GetHarvestLogsEndpoint.Map(api);

// Pest & Disease Logs
LogPestDiseaseEndpoint.Map(api);
GetPestDiseaseLogsEndpoint.Map(api);
ResolvePestDiseaseLogEndpoint.Map(api);

// Amendment Logs
LogAmendmentEndpoint.Map(api);
GetAmendmentLogsEndpoint.Map(api);

// Weather Observations
LogWeatherObservationEndpoint.Map(api);
GetWeatherObservationsEndpoint.Map(api);

// User Insights
GetUserInsightsEndpoint.Map(api);
MarkInsightReadEndpoint.Map(api);
CreateUserInsightEndpoint.Map(api);

app.Run();

public partial class Program { }
