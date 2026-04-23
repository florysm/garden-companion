using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Gardens;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetGardenTypesQuery : IRequest<List<GardenTypeDto>>;

public record GardenTypeDto(int Id, string Name);

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetGardenTypesHandler(AppDbContext db)
    : IRequestHandler<GetGardenTypesQuery, List<GardenTypeDto>>
{
    public async Task<List<GardenTypeDto>> Handle(
        GetGardenTypesQuery request, CancellationToken cancellationToken) =>
        await db.GardenTypes
            .OrderBy(t => t.Name)
            .Select(t => new GardenTypeDto(t.Id, t.Name))
            .ToListAsync(cancellationToken);
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetGardenTypesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/garden-types", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetGardenTypesQuery(), ct)))
        .RequireAuthorization()
        .WithTags("Gardens")
        .WithName("GetGardenTypes");
    }
}
