using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Gardens;

// ── Request / Response ──────────────────────────────────────────────────────

public record GetGardensQuery(Guid CurrentUserId, Guid? HouseholdId) : IRequest<List<GardenSummaryDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetGardensHandler(AppDbContext db)
    : IRequestHandler<GetGardensQuery, List<GardenSummaryDto>>
{
    public async Task<List<GardenSummaryDto>> Handle(
        GetGardensQuery request, CancellationToken cancellationToken)
    {
        var userId = request.CurrentUserId;

        var query = db.Gardens
            .Where(g =>
                g.Members.Any(m => m.UserId == userId) ||
                g.Household.Members.Any(m => m.UserId == userId));

        if (request.HouseholdId.HasValue)
            query = query.Where(g => g.HouseholdId == request.HouseholdId.Value);

        var gardens = await query
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                Types = g.GardenTypes.Select(t => t.Name).ToList(),
                BedCount = g.Beds.Count,
                DirectRole = g.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (GardenRole?)m.Role)
                    .FirstOrDefault(),
                g.CreatedAt
            })
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return gardens.Select(g => new GardenSummaryDto(
            g.Id,
            g.Name,
            g.Description,
            g.Types,
            g.BedCount,
            (g.DirectRole ?? GardenRole.Contributor).ToString(),
            g.CreatedAt))
        .ToList();
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetGardensEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens", async (
            Guid? householdId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            var result = await mediator.Send(new GetGardensQuery(userId, householdId), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Gardens")
        .WithName("GetGardens");
    }
}
