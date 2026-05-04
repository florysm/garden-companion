using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Infrastructure.Data;
using GardenCompanion.Api.Infrastructure.ExternalData.Weather;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.WeatherObservations;

public record RefreshLatestWeatherObservationCommand(Guid HouseholdId, Guid UserId)
    : IRequest<WeatherObservationDto?>;

public class RefreshLatestWeatherObservationHandler(
    AppDbContext db,
    IEnumerable<IWeatherProvider> providers,
    ILogger<RefreshLatestWeatherObservationHandler> logger)
    : IRequestHandler<RefreshLatestWeatherObservationCommand, WeatherObservationDto?>
{
    public async Task<WeatherObservationDto?> Handle(
        RefreshLatestWeatherObservationCommand request,
        CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireMemberAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var household = await db.Households
            .Include(h => h.WeatherStationIntegration)
            .FirstOrDefaultAsync(h => h.Id == request.HouseholdId, cancellationToken)
            ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        var station = household.WeatherStationIntegration;
        if (station is null)
            return null;

        var provider = providers.FirstOrDefault(p => p.ProviderType == station.Provider)
            ?? throw new InvalidOperationException($"No provider registered for {station.Provider}.");

        var data = await provider.FetchAsync(station, household, cancellationToken);
        if (data is null)
            throw new InvalidOperationException("Provider returned no data. Check your station ID and credentials.");

        var observation = new WeatherObservation
        {
            Id = Guid.NewGuid(),
            HouseholdId = station.HouseholdId,
            ObservedAt = data.ObservedAt,
            TemperatureF = data.TemperatureF,
            Humidity = data.Humidity,
            WindSpeedMph = data.WindSpeedMph,
            WindDirectionDegrees = data.WindDirectionDegrees,
            PrecipitationRateInPerHr = data.PrecipitationRateInPerHr,
            PrecipitationTotalIn = data.PrecipitationTotalIn,
            UvIndex = data.UvIndex,
            DewPointF = data.DewPointF,
            PressureInHg = data.PressureInHg,
            Source = station.Provider,
            StationId = data.StationId,
        };

        db.WeatherObservations.Add(observation);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Refreshed latest weather observation for household {HouseholdId}. Provider={Provider}.",
            request.HouseholdId,
            station.Provider);

        return new WeatherObservationDto(
            observation.Id,
            observation.HouseholdId,
            observation.ObservedAt,
            observation.TemperatureF,
            observation.Humidity,
            observation.WindSpeedMph,
            observation.WindDirectionDegrees,
            observation.PrecipitationRateInPerHr,
            observation.PrecipitationTotalIn,
            observation.UvIndex,
            observation.DewPointF,
            observation.PressureInHg,
            observation.Source,
            observation.StationId);
    }
}

public static class RefreshLatestWeatherObservationEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/households/{householdId:guid}/weather/refresh", async (
            Guid householdId,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new RefreshLatestWeatherObservationCommand(householdId, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }
        })
        .RequireAuthorization()
        .WithTags("WeatherObservations")
        .WithName("RefreshLatestWeatherObservation");
    }
}
