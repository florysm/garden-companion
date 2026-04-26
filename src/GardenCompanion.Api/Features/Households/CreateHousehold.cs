using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request / Response ───────────────────────────────────────────────────────

public record CreateHouseholdCommand(Guid UserId, string Name) : IRequest<HouseholdDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record CreateHouseholdBody(string Name);

public class CreateHouseholdValidator : AbstractValidator<CreateHouseholdBody>
{
    public CreateHouseholdValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class CreateHouseholdHandler(AppDbContext db)
    : IRequestHandler<CreateHouseholdCommand, HouseholdDto>
{
    public async Task<HouseholdDto> Handle(
        CreateHouseholdCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        var now = DateTime.UtcNow;
        var householdId = Guid.NewGuid();

        var household = new Household
        {
            Id = householdId,
            Name = request.Name,
            OwnedByUserId = request.UserId,
            CreatedAt = now
        };

        var ownerMember = new HouseholdMember
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            UserId = request.UserId,
            Role = HouseholdRole.Owner,
            JoinedAt = now
        };

        db.Households.Add(household);
        db.HouseholdMembers.Add(ownerMember);
        await db.SaveChangesAsync(cancellationToken);

        return new HouseholdDto(
            household.Id,
            household.Name,
            household.OwnedByUserId,
            user.DisplayName,
            household.CreatedAt,
            [new HouseholdMemberDto(user.Id, user.DisplayName, user.Email, HouseholdRole.Owner, now)],
            false);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class CreateHouseholdEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/households", async (
            CreateHouseholdBody body,
            IValidator<CreateHouseholdBody> validator,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new CreateHouseholdCommand(userId, body.Name), ct);
                return Results.Created($"/api/households/{result.Id}", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("CreateHousehold");
    }
}
