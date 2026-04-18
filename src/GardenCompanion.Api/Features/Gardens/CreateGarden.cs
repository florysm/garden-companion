using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Gardens;

// ── Request / Response ──────────────────────────────────────────────────────

public record CreateGardenCommand(
    Guid CurrentUserId,
    Guid HouseholdId,
    string Name,
    string? Description,
    List<int> GardenTypeIds) : IRequest<GardenDetailDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public class CreateGardenValidator : AbstractValidator<CreateGardenCommand>
{
    public CreateGardenValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.GardenTypeIds).NotNull();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class CreateGardenHandler(AppDbContext db)
    : IRequestHandler<CreateGardenCommand, GardenDetailDto>
{
    public async Task<GardenDetailDto> Handle(
        CreateGardenCommand request, CancellationToken cancellationToken)
    {
        var isMember = await db.HouseholdMembers
            .AnyAsync(m => m.HouseholdId == request.HouseholdId
                        && m.UserId == request.CurrentUserId, cancellationToken);

        if (!isMember)
            throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        var types = await db.GardenTypes
            .Where(t => request.GardenTypeIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var garden = new Garden
        {
            Id = Guid.NewGuid(),
            HouseholdId = request.HouseholdId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = now,
            GardenTypes = types
        };

        var membership = new GardenMember
        {
            Id = Guid.NewGuid(),
            GardenId = garden.Id,
            UserId = request.CurrentUserId,
            Role = GardenRole.Owner
        };

        db.Gardens.Add(garden);
        db.GardenMembers.Add(membership);
        await db.SaveChangesAsync(cancellationToken);

        var user = await db.Users.FindAsync([request.CurrentUserId], cancellationToken);

        return new GardenDetailDto(
            garden.Id,
            garden.Name,
            garden.Description,
            types.Select(t => t.Name).ToList(),
            [],
            [new GardenMemberDto(request.CurrentUserId, user!.DisplayName, GardenRole.Owner.ToString())],
            GardenRole.Owner.ToString(),
            garden.CreatedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class CreateGardenEndpoint
{
    public record Request(Guid HouseholdId, string Name, string? Description, List<int> GardenTypeIds);

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/gardens", async (
            Request req,
            HttpContext ctx,
            IValidator<CreateGardenCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            var command = new CreateGardenCommand(userId, req.HouseholdId, req.Name, req.Description, req.GardenTypeIds ?? []);

            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/gardens/{result.Id}", result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithTags("Gardens")
        .WithName("CreateGarden");
    }
}
