using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.UserInsights;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record UserInsightDto(
    Guid Id,
    Guid HouseholdId,
    Guid? GardenId,
    Guid? GardenBedId,
    InsightType InsightType,
    string Title,
    string Body,
    bool IsRead,
    DateTime? ExpiresAt,
    DateTime GeneratedAt);

// ── Request / Response ───────────────────────────────────────────────────────

public record GetUserInsightsQuery(
    Guid HouseholdId,
    Guid UserId,
    bool? IsRead,
    InsightType? InsightType,
    Guid? GardenId) : IRequest<List<UserInsightDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetUserInsightsHandler(AppDbContext db)
    : IRequestHandler<GetUserInsightsQuery, List<UserInsightDto>>
{
    public async Task<List<UserInsightDto>> Handle(
        GetUserInsightsQuery request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireMemberAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var now = DateTime.UtcNow;
        var query = db.UserInsights
            .Where(i => i.HouseholdId == request.HouseholdId)
            .Where(i => i.ExpiresAt == null || i.ExpiresAt > now);

        if (request.IsRead.HasValue)
            query = query.Where(i => i.IsRead == request.IsRead.Value);

        if (request.InsightType.HasValue)
            query = query.Where(i => i.InsightType == request.InsightType.Value);

        if (request.GardenId.HasValue)
            query = query.Where(i => i.GardenId == request.GardenId.Value);

        return await query
            .OrderByDescending(i => i.GeneratedAt)
            .Select(i => new UserInsightDto(
                i.Id, i.HouseholdId, i.GardenId, i.GardenBedId,
                i.InsightType, i.Title, i.Body, i.IsRead, i.ExpiresAt, i.GeneratedAt))
            .ToListAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetUserInsightsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/households/{householdId:guid}/insights", async (
            Guid householdId,
            bool? isRead,
            InsightType? insightType,
            Guid? gardenId,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new GetUserInsightsQuery(householdId, userId, isRead, insightType, gardenId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("UserInsights")
        .WithName("GetUserInsights");
    }
}
