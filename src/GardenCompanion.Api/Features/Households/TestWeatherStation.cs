using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using GardenCompanion.Api.Infrastructure.ExternalData.Weather;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request / Response ───────────────────────────────────────────────────────

public record TestWeatherStationCommand(
    Guid HouseholdId,
    Guid UserId,
    WeatherProvider Provider,
    string? StationId,
    string? ApiKey) : IRequest<WeatherTestResultDto>;

public record WeatherTestResultDto(
    decimal TemperatureF,
    decimal Humidity,
    decimal WindSpeedMph,
    int? WindDirectionDegrees,
    decimal PrecipitationRateInPerHr,
    decimal? UvIndex,
    decimal? DewPointF,
    decimal? PressureInHg,
    string? StationId);

// ── Handler ──────────────────────────────────────────────────────────────────

public class TestWeatherStationHandler(AppDbContext db, IEnumerable<IWeatherProvider> providers)
    : IRequestHandler<TestWeatherStationCommand, WeatherTestResultDto>
{
    public async Task<WeatherTestResultDto> Handle(
        TestWeatherStationCommand request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireOwnerAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var household = await db.Households
            .Include(h => h.WeatherStationIntegration)
            .FirstOrDefaultAsync(h => h.Id == request.HouseholdId, cancellationToken)
            ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        // If caller omitted the API key, fall back to the saved key so the user
        // doesn't need to re-enter it just to run a test.
        var resolvedApiKey = request.ApiKey
            ?? household.WeatherStationIntegration?.ApiKey;

        var tempStation = new WeatherStationIntegration
        {
            Id = Guid.Empty,
            HouseholdId = request.HouseholdId,
            Provider = request.Provider,
            StationId = request.StationId,
            ApiKey = resolvedApiKey,
            CreatedAt = DateTime.UtcNow,
            Household = household,
        };

        var provider = providers.FirstOrDefault(p => p.ProviderType == request.Provider)
            ?? throw new InvalidOperationException($"No provider registered for {request.Provider}.");

        var data = await provider.FetchAsync(tempStation, household, cancellationToken);

        if (data is null)
            throw new InvalidOperationException("Provider returned no data. Check your station ID and credentials.");

        return new WeatherTestResultDto(
            data.TemperatureF,
            data.Humidity,
            data.WindSpeedMph,
            data.WindDirectionDegrees,
            data.PrecipitationRateInPerHr,
            data.UvIndex,
            data.DewPointF,
            data.PressureInHg,
            data.StationId);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class TestWeatherStationEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/households/{householdId:guid}/weather-station/test", async (
            Guid householdId,
            UpsertWeatherStationBody body,
            IValidator<UpsertWeatherStationBody> validator,
            HttpContext ctx,
            ILogger<TestWeatherStationHandler> logger,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            logger.LogInformation(
                "Testing weather station connection for household {HouseholdId}. Provider={Provider}, HasStationId={HasStationId}, HasApiKey={HasApiKey}.",
                householdId,
                body.Provider,
                !string.IsNullOrWhiteSpace(body.StationId),
                !string.IsNullOrWhiteSpace(body.ApiKey));

            try
            {
                var result = await mediator.Send(
                    new TestWeatherStationCommand(householdId, userId, body.Provider, body.StationId, body.ApiKey), ct);
                logger.LogInformation(
                    "Weather station test succeeded for household {HouseholdId}. Provider={Provider}.",
                    householdId,
                    body.Provider);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(
                    ex,
                    "Weather station test failed for household {HouseholdId}. Provider={Provider}.",
                    householdId,
                    body.Provider);
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("TestWeatherStation");
    }
}
