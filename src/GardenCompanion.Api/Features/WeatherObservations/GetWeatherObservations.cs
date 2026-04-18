using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.WeatherObservations;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetWeatherObservationsQuery(
    Guid HouseholdId,
    Guid UserId,
    DateTime? From,
    DateTime? To,
    int Limit) : IRequest<List<WeatherObservationDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetWeatherObservationsHandler(AppDbContext db)
    : IRequestHandler<GetWeatherObservationsQuery, List<WeatherObservationDto>>
{
    public async Task<List<WeatherObservationDto>> Handle(
        GetWeatherObservationsQuery request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireMemberAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var query = db.WeatherObservations
            .Where(o => o.HouseholdId == request.HouseholdId);

        if (request.From.HasValue)
            query = query.Where(o => o.ObservedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(o => o.ObservedAt <= request.To.Value);

        return await query
            .OrderByDescending(o => o.ObservedAt)
            .Take(request.Limit)
            .Select(o => new WeatherObservationDto(
                o.Id, o.HouseholdId, o.ObservedAt, o.TemperatureF, o.Humidity,
                o.WindSpeedMph, o.WindDirectionDegrees, o.PrecipitationRateInPerHr,
                o.PrecipitationTotalIn, o.UvIndex, o.DewPointF, o.PressureInHg,
                o.Source, o.StationId))
            .ToListAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetWeatherObservationsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/households/{householdId:guid}/weather", async (
            Guid householdId,
            DateTime? from,
            DateTime? to,
            int? limit,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new GetWeatherObservationsQuery(householdId, userId, from, to, limit ?? 500), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("WeatherObservations")
        .WithName("GetWeatherObservations");
    }
}
