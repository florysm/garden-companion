using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Users;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetMyProfileQuery(Guid UserId) : IRequest<UserProfileDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetMyProfileHandler(AppDbContext db)
    : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(
        GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        return await db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserProfileDto(u.Id, u.Email, u.DisplayName, u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetMyProfileEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/users/me", async (
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            var result = await mediator.Send(new GetMyProfileQuery(userId), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Users")
        .WithName("GetMyProfile");
    }
}
