using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Gardens;

// ── Request / Response ──────────────────────────────────────────────────────

public record UpdateGardenCommand(
    Guid GardenId,
    Guid CurrentUserId,
    string Name,
    string? Description,
    List<int> GardenTypeIds) : IRequest<GardenDetailDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public class UpdateGardenValidator : AbstractValidator<UpdateGardenCommand>
{
    public UpdateGardenValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.GardenTypeIds).NotNull();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpdateGardenHandler(AppDbContext db)
    : IRequestHandler<UpdateGardenCommand, GardenDetailDto>
{
    public async Task<GardenDetailDto> Handle(
        UpdateGardenCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var garden = await db.Gardens
            .Include(g => g.GardenTypes)
            .Include(g => g.Members).ThenInclude(m => m.User)
            .Include(g => g.Beds).ThenInclude(b => b.Plantings)
            .FirstOrDefaultAsync(g => g.Id == request.GardenId, cancellationToken)
            ?? throw new KeyNotFoundException($"Garden {request.GardenId} not found.");

        var newTypes = await db.GardenTypes
            .Where(t => request.GardenTypeIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        garden.Name = request.Name;
        garden.Description = request.Description;
        garden.GardenTypes.Clear();
        foreach (var t in newTypes) garden.GardenTypes.Add(t);

        await db.SaveChangesAsync(cancellationToken);

        var role = await GardenAccess.GetRoleAsync(
            db, garden.Id, request.CurrentUserId, cancellationToken);

        return new GardenDetailDto(
            garden.Id,
            garden.Name,
            garden.Description,
            garden.GardenTypes.Select(t => t.Name).ToList(),
            garden.Beds.Select(b => new GardenBedSummaryDto(
                b.Id, b.Name, b.Type.ToString(), b.Shape.ToString(),
                b.SunExposure.ToString(), b.Plantings.Count(p => p.IsActive))).ToList(),
            garden.Members.Select(m => new GardenMemberDto(
                m.UserId, m.User.DisplayName, m.Role.ToString())).ToList(),
            role!.Value.ToString(),
            garden.CreatedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpdateGardenEndpoint
{
    public record Request(string Name, string? Description, List<int> GardenTypeIds);

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/gardens/{id:guid}", async (
            Guid id,
            Request req,
            HttpContext ctx,
            IValidator<UpdateGardenCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            var command = new UpdateGardenCommand(id, userId, req.Name, req.Description, req.GardenTypeIds ?? []);

            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization()
        .WithTags("Gardens")
        .WithName("UpdateGarden");
    }
}
