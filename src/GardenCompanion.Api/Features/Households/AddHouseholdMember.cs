using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request / Response ───────────────────────────────────────────────────────

public record AddHouseholdMemberCommand(
    Guid HouseholdId,
    Guid CurrentUserId,
    string Email,
    HouseholdRole Role) : IRequest<HouseholdMemberDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record AddHouseholdMemberBody(string Email, HouseholdRole Role);

public class AddHouseholdMemberValidator : AbstractValidator<AddHouseholdMemberBody>
{
    public AddHouseholdMemberValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class AddHouseholdMemberHandler(AppDbContext db)
    : IRequestHandler<AddHouseholdMemberCommand, HouseholdMemberDto>
{
    public async Task<HouseholdMemberDto> Handle(
        AddHouseholdMemberCommand request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireOwnerAsync(db, request.HouseholdId, request.CurrentUserId, cancellationToken);

        var targetUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken)
            ?? throw new KeyNotFoundException($"No user found with email '{request.Email}'.");

        var alreadyMember = await db.HouseholdMembers
            .AnyAsync(m => m.HouseholdId == request.HouseholdId && m.UserId == targetUser.Id, cancellationToken);

        if (alreadyMember)
            throw new InvalidOperationException($"'{request.Email}' is already a member of this household.");

        var now = DateTime.UtcNow;
        var member = new HouseholdMember
        {
            Id = Guid.NewGuid(),
            HouseholdId = request.HouseholdId,
            UserId = targetUser.Id,
            Role = request.Role,
            JoinedAt = now
        };

        db.HouseholdMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return new HouseholdMemberDto(
            targetUser.Id,
            targetUser.DisplayName,
            targetUser.Email,
            request.Role,
            now);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class AddHouseholdMemberEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/households/{householdId:guid}/members", async (
            Guid householdId,
            AddHouseholdMemberBody body,
            IValidator<AddHouseholdMemberBody> validator,
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
                var result = await mediator.Send(
                    new AddHouseholdMemberCommand(householdId, userId, body.Email, body.Role), ct);
                return Results.Created($"/api/households/{householdId}/members", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("AddHouseholdMember");
    }
}
