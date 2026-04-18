using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.SoilTests;

// ── Request ──────────────────────────────────────────────────────────────────

public record GetSoilTestQuery(Guid TestId, Guid GardenId, Guid BedId, Guid CurrentUserId) : IRequest<SoilTestDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetSoilTestHandler(AppDbContext db)
    : IRequestHandler<GetSoilTestQuery, SoilTestDto>
{
    public async Task<SoilTestDto> Handle(
        GetSoilTestQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireBedMemberAsync(
            db, request.BedId, request.CurrentUserId, cancellationToken);

        var test = await db.SoilTests
            .FirstOrDefaultAsync(
                t => t.Id == request.TestId && t.GardenBedId == request.BedId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Soil test {request.TestId} not found.");

        return CreateSoilTestHandler.ToDto(test);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetSoilTestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{gardenId:guid}/beds/{bedId:guid}/soil-tests/{testId:guid}", async (
            Guid gardenId,
            Guid bedId,
            Guid testId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new GetSoilTestQuery(testId, gardenId, bedId, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("SoilTests")
        .WithName("GetSoilTest");
    }
}
