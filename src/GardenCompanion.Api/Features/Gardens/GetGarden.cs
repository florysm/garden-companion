using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Gardens;

// ── Request ──────────────────────────────────────────────────────────────────

public record GetGardenQuery(Guid GardenId, Guid CurrentUserId) : IRequest<GardenDetailDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetGardenHandler(AppDbContext db)
    : IRequestHandler<GetGardenQuery, GardenDetailDto>
{
    public async Task<GardenDetailDto> Handle(
        GetGardenQuery request, CancellationToken cancellationToken)
    {
        var role = await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var garden = await db.Gardens
            .Where(g => g.Id == request.GardenId)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Description,
                Types = g.GardenTypes.Select(t => t.Name).ToList(),
                Beds = g.Beds.Select(b => new GardenBedSummaryDto(
                    b.Id,
                    b.Name,
                    b.Type.ToString(),
                    b.Shape.ToString(),
                    b.SunExposure.ToString(),
                    b.Plantings.Count(p => p.IsActive))).ToList(),
                Members = g.Members.Select(m => new GardenMemberDto(
                    m.UserId,
                    m.User.DisplayName,
                    m.Role.ToString())).ToList(),
                g.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Garden {request.GardenId} not found.");

        return new GardenDetailDto(
            garden.Id,
            garden.Name,
            garden.Description,
            garden.Types,
            garden.Beds,
            garden.Members,
            role.ToString(),
            garden.CreatedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetGardenEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{id:guid}", async (
            Guid id,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new GetGardenQuery(id, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("Gardens")
        .WithName("GetGarden");
    }
}
