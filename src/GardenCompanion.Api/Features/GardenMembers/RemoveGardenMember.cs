using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenMembers;

// ── Request ──────────────────────────────────────────────────────────────────

public record RemoveGardenMemberCommand(
    Guid CurrentUserId,
    Guid GardenId,
    Guid TargetUserId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public class RemoveGardenMemberHandler(AppDbContext db)
    : IRequestHandler<RemoveGardenMemberCommand>
{
    public async Task Handle(RemoveGardenMemberCommand request, CancellationToken cancellationToken)
    {
        // Allow an owner to remove others, or any member to remove themselves.
        if (request.CurrentUserId != request.TargetUserId)
        {
            await GardenAccess.RequireOwnerAsync(
                db, request.GardenId, request.CurrentUserId, cancellationToken);
        }
        else
        {
            await GardenAccess.RequireMemberAsync(
                db, request.GardenId, request.CurrentUserId, cancellationToken);
        }

        var member = await db.GardenMembers
            .FirstOrDefaultAsync(
                m => m.GardenId == request.GardenId && m.UserId == request.TargetUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Member not found in this garden.");

        // Prevent removing the last Owner.
        if (member.Role == Domain.Enums.GardenRole.Owner)
        {
            var ownerCount = await db.GardenMembers
                .CountAsync(
                    m => m.GardenId == request.GardenId
                      && m.Role == Domain.Enums.GardenRole.Owner,
                    cancellationToken);

            if (ownerCount <= 1)
                throw new InvalidOperationException(
                    "Cannot remove the last owner. Transfer ownership first.");
        }

        db.GardenMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class RemoveGardenMemberEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/gardens/{gardenId:guid}/members/{targetUserId:guid}", async (
            Guid gardenId,
            Guid targetUserId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                await mediator.Send(new RemoveGardenMemberCommand(userId, gardenId, targetUserId), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("GardenMembers")
        .WithName("RemoveGardenMember");
    }
}
