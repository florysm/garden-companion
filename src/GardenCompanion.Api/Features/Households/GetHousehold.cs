using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetHouseholdQuery(Guid HouseholdId, Guid UserId) : IRequest<HouseholdDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetHouseholdHandler(AppDbContext db)
    : IRequestHandler<GetHouseholdQuery, HouseholdDto>
{
    public async Task<HouseholdDto> Handle(
        GetHouseholdQuery request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireMemberAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var household = await db.Households
            .Where(h => h.Id == request.HouseholdId)
            .Select(h => new
            {
                h.Id,
                h.Name,
                h.OwnedByUserId,
                OwnerDisplayName = h.Owner.DisplayName,
                h.CreatedAt,
                h.WeatherStationIntegrationId,
                Members = h.Members.Select(m => new HouseholdMemberDto(
                    m.UserId,
                    m.User.DisplayName,
                    m.User.Email,
                    m.Role,
                    m.JoinedAt)).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        return new HouseholdDto(
            household.Id,
            household.Name,
            household.OwnedByUserId,
            household.OwnerDisplayName,
            household.CreatedAt,
            household.Members,
            household.WeatherStationIntegrationId.HasValue);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetHouseholdEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/households/{householdId:guid}", async (
            Guid householdId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new GetHouseholdQuery(householdId, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("GetHousehold");
    }
}
