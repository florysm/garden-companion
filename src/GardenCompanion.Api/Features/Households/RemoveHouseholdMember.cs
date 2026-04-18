using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request ──────────────────────────────────────────────────────────────────

public record RemoveHouseholdMemberCommand(
    Guid HouseholdId,
    Guid CurrentUserId,
    Guid TargetUserId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public class RemoveHouseholdMemberHandler(AppDbContext db)
    : IRequestHandler<RemoveHouseholdMemberCommand>
{
    public async Task Handle(RemoveHouseholdMemberCommand request, CancellationToken cancellationToken)
    {
        var currentRole = await HouseholdAccess.GetRoleAsync(
            db, request.HouseholdId, request.CurrentUserId, cancellationToken);

        if (currentRole is null)
            throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        // Owner cannot be removed
        var targetMember = await db.HouseholdMembers
            .FirstOrDefaultAsync(
                m => m.HouseholdId == request.HouseholdId && m.UserId == request.TargetUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Member not found in household.");

        if (targetMember.Role == HouseholdRole.Owner)
            throw new InvalidOperationException("The household owner cannot be removed.");

        // Only the owner can remove others; members can only remove themselves
        var isSelf = request.CurrentUserId == request.TargetUserId;
        if (!isSelf && currentRole != HouseholdRole.Owner)
            throw new UnauthorizedAccessException("Only the household owner can remove other members.");

        db.HouseholdMembers.Remove(targetMember);
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class RemoveHouseholdMemberEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/households/{householdId:guid}/members/{userId:guid}", async (
            Guid householdId,
            Guid userId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var currentUserId = ctx.User.GetUserId();
            try
            {
                await mediator.Send(
                    new RemoveHouseholdMemberCommand(householdId, currentUserId, userId), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("RemoveHouseholdMember");
    }
}
