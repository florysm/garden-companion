using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request ──────────────────────────────────────────────────────────────────

public record DeleteWeatherStationCommand(Guid HouseholdId, Guid UserId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public class DeleteWeatherStationHandler(AppDbContext db)
    : IRequestHandler<DeleteWeatherStationCommand>
{
    public async Task Handle(DeleteWeatherStationCommand request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireOwnerAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var household = await db.Households
            .Include(h => h.WeatherStationIntegration)
            .FirstOrDefaultAsync(h => h.Id == request.HouseholdId, cancellationToken)
            ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        if (household.WeatherStationIntegration is null)
            throw new KeyNotFoundException($"No weather station configured for household {request.HouseholdId}.");

        // Break the circular FK before deleting to satisfy the constraint
        var station = household.WeatherStationIntegration;
        household.WeatherStationIntegrationId = null;
        await db.SaveChangesAsync(cancellationToken);

        db.WeatherStationIntegrations.Remove(station);
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class DeleteWeatherStationEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/households/{householdId:guid}/weather-station", async (
            Guid householdId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                await mediator.Send(new DeleteWeatherStationCommand(householdId, userId), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("DeleteWeatherStation");
    }
}
