using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;

namespace GardenCompanion.Api.Features.WeatherObservations;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record WeatherObservationDto(
    Guid Id,
    Guid HouseholdId,
    DateTime ObservedAt,
    decimal TemperatureF,
    decimal Humidity,
    decimal WindSpeedMph,
    int? WindDirectionDegrees,
    decimal PrecipitationRateInPerHr,
    decimal PrecipitationTotalIn,
    decimal? UvIndex,
    decimal? DewPointF,
    decimal? PressureInHg,
    WeatherProvider Source,
    string? StationId);

// ── Request / Response ───────────────────────────────────────────────────────

public record LogWeatherObservationCommand(
    Guid HouseholdId,
    Guid UserId,
    DateTime? ObservedAt,
    decimal TemperatureF,
    decimal Humidity,
    decimal WindSpeedMph,
    int? WindDirectionDegrees,
    decimal PrecipitationRateInPerHr,
    decimal PrecipitationTotalIn,
    decimal? UvIndex,
    decimal? DewPointF,
    decimal? PressureInHg,
    WeatherProvider Source,
    string? StationId) : IRequest<WeatherObservationDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record LogWeatherObservationBody(
    DateTime? ObservedAt,
    decimal TemperatureF,
    decimal Humidity,
    decimal WindSpeedMph,
    int? WindDirectionDegrees,
    decimal PrecipitationRateInPerHr,
    decimal PrecipitationTotalIn,
    decimal? UvIndex,
    decimal? DewPointF,
    decimal? PressureInHg,
    WeatherProvider Source,
    string? StationId);

public class LogWeatherObservationValidator : AbstractValidator<LogWeatherObservationBody>
{
    public LogWeatherObservationValidator()
    {
        RuleFor(x => x.Humidity).InclusiveBetween(0, 100);
        RuleFor(x => x.WindSpeedMph).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WindDirectionDegrees).InclusiveBetween(0, 359).When(x => x.WindDirectionDegrees.HasValue);
        RuleFor(x => x.PrecipitationRateInPerHr).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PrecipitationTotalIn).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UvIndex).GreaterThanOrEqualTo(0).When(x => x.UvIndex.HasValue);
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.StationId).MaximumLength(100).When(x => x.StationId is not null);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class LogWeatherObservationHandler(AppDbContext db)
    : IRequestHandler<LogWeatherObservationCommand, WeatherObservationDto>
{
    public async Task<WeatherObservationDto> Handle(
        LogWeatherObservationCommand request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireMemberAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var observation = new WeatherObservation
        {
            Id = Guid.NewGuid(),
            HouseholdId = request.HouseholdId,
            ObservedAt = request.ObservedAt ?? DateTime.UtcNow,
            TemperatureF = request.TemperatureF,
            Humidity = request.Humidity,
            WindSpeedMph = request.WindSpeedMph,
            WindDirectionDegrees = request.WindDirectionDegrees,
            PrecipitationRateInPerHr = request.PrecipitationRateInPerHr,
            PrecipitationTotalIn = request.PrecipitationTotalIn,
            UvIndex = request.UvIndex,
            DewPointF = request.DewPointF,
            PressureInHg = request.PressureInHg,
            Source = request.Source,
            StationId = request.StationId
        };

        db.WeatherObservations.Add(observation);
        await db.SaveChangesAsync(cancellationToken);

        return new WeatherObservationDto(
            observation.Id, observation.HouseholdId, observation.ObservedAt,
            observation.TemperatureF, observation.Humidity, observation.WindSpeedMph,
            observation.WindDirectionDegrees, observation.PrecipitationRateInPerHr,
            observation.PrecipitationTotalIn, observation.UvIndex, observation.DewPointF,
            observation.PressureInHg, observation.Source, observation.StationId);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class LogWeatherObservationEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/households/{householdId:guid}/weather", async (
            Guid householdId,
            LogWeatherObservationBody body,
            IValidator<LogWeatherObservationBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new LogWeatherObservationCommand(
                householdId, userId, body.ObservedAt, body.TemperatureF, body.Humidity,
                body.WindSpeedMph, body.WindDirectionDegrees, body.PrecipitationRateInPerHr,
                body.PrecipitationTotalIn, body.UvIndex, body.DewPointF, body.PressureInHg,
                body.Source, body.StationId);

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/households/{householdId}/weather/{result.Id}", result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("WeatherObservations")
        .WithName("LogWeatherObservation");
    }
}
