using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenBeds;

// ── Request ──────────────────────────────────────────────────────────────────

public record GetGardenBedQuery(Guid GardenId, Guid BedId, Guid CurrentUserId)
    : IRequest<GardenBedDetailDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetGardenBedHandler(AppDbContext db)
    : IRequestHandler<GetGardenBedQuery, GardenBedDetailDto>
{
    public async Task<GardenBedDetailDto> Handle(
        GetGardenBedQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var bed = await db.GardenBeds
            .Where(b => b.Id == request.BedId && b.GardenId == request.GardenId)
            .Select(b => new
            {
                Bed = b,
                ActivePlantings = b.Plantings.Count(p => p.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Bed {request.BedId} not found.");

        return CreateGardenBedHandler.ToDto(bed.Bed, bed.ActivePlantings);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetGardenBedEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{gardenId:guid}/beds/{bedId:guid}", async (
            Guid gardenId,
            Guid bedId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new GetGardenBedQuery(gardenId, bedId, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("GardenBeds")
        .WithName("GetGardenBed");
    }
}
