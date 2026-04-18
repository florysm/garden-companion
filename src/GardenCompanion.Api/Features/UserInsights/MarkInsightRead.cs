using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.UserInsights;

// ── Request / Response ───────────────────────────────────────────────────────

public record MarkInsightReadCommand(Guid HouseholdId, Guid InsightId, Guid UserId)
    : IRequest<UserInsightDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class MarkInsightReadHandler(AppDbContext db)
    : IRequestHandler<MarkInsightReadCommand, UserInsightDto>
{
    public async Task<UserInsightDto> Handle(
        MarkInsightReadCommand request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireMemberAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var insight = await db.UserInsights
            .FirstOrDefaultAsync(i => i.Id == request.InsightId && i.HouseholdId == request.HouseholdId, cancellationToken)
            ?? throw new KeyNotFoundException($"Insight {request.InsightId} not found.");

        insight.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);

        return new UserInsightDto(
            insight.Id, insight.HouseholdId, insight.GardenId, insight.GardenBedId,
            insight.InsightType, insight.Title, insight.Body, insight.IsRead,
            insight.ExpiresAt, insight.GeneratedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class MarkInsightReadEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/households/{householdId:guid}/insights/{id:guid}/read", async (
            Guid householdId,
            Guid id,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new MarkInsightReadCommand(householdId, id, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("UserInsights")
        .WithName("MarkInsightRead");
    }
}
