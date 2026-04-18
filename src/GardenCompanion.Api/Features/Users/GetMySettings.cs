using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Users;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetMySettingsQuery(Guid UserId) : IRequest<UserSettingsDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetMySettingsHandler(AppDbContext db)
    : IRequestHandler<GetMySettingsQuery, UserSettingsDto>
{
    public async Task<UserSettingsDto> Handle(
        GetMySettingsQuery request, CancellationToken cancellationToken)
    {
        return await db.UserSettings
            .Where(s => s.UserId == request.UserId)
            .Select(s => new UserSettingsDto(
                s.Id,
                s.LocationLatitude,
                s.LocationLongitude,
                s.PreferredLanguage,
                s.TemperatureUnit,
                s.LengthUnit,
                s.WeightUnit,
                s.VolumeUnit,
                s.UsdaHardinessZone,
                s.AverageFrostDateSpring,
                s.AverageFrostDateFall,
                s.ShareWeatherData,
                s.SharePlantingData,
                s.ShareHarvestData))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Settings not found for user {request.UserId}.");
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetMySettingsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/me/settings", async (
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            var result = await mediator.Send(new GetMySettingsQuery(userId), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Users")
        .WithName("GetMySettings");
    }
}
