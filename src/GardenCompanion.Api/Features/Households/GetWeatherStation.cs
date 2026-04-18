using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetWeatherStationQuery(Guid HouseholdId, Guid UserId) : IRequest<WeatherStationDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetWeatherStationHandler(AppDbContext db)
    : IRequestHandler<GetWeatherStationQuery, WeatherStationDto>
{
    public async Task<WeatherStationDto> Handle(
        GetWeatherStationQuery request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireMemberAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var station = await db.WeatherStationIntegrations
            .Where(w => w.HouseholdId == request.HouseholdId)
            .Select(w => new WeatherStationDto(
                w.Id,
                w.Provider,
                w.StationId,
                w.ApiKey != null,
                w.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"No weather station configured for household {request.HouseholdId}.");

        return station;
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetWeatherStationEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/households/{householdId:guid}/weather-station", async (
            Guid householdId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new GetWeatherStationQuery(householdId, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("GetWeatherStation");
    }
}
