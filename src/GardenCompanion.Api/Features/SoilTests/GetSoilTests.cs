using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.SoilTests;

// ── Request ──────────────────────────────────────────────────────────────────

public record GetSoilTestsQuery(
    Guid CurrentUserId,
    Guid GardenId,
    Guid BedId) : IRequest<List<SoilTestDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetSoilTestsHandler(AppDbContext db)
    : IRequestHandler<GetSoilTestsQuery, List<SoilTestDto>>
{
    public async Task<List<SoilTestDto>> Handle(
        GetSoilTestsQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireBedMemberAsync(
            db, request.BedId, request.CurrentUserId, cancellationToken);

        var tests = await db.SoilTests
            .Where(t => t.GardenBedId == request.BedId)
            .OrderByDescending(t => t.TestedAt)
            .ToListAsync(cancellationToken);

        return tests.Select(CreateSoilTestHandler.ToDto).ToList();
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetSoilTestsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{gardenId:guid}/beds/{bedId:guid}/soil-tests", async (
            Guid gardenId,
            Guid bedId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new GetSoilTestsQuery(userId, gardenId, bedId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("SoilTests")
        .WithName("GetSoilTests");
    }
}
