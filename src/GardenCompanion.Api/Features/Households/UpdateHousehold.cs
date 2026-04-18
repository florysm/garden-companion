using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request / Response ───────────────────────────────────────────────────────

public record UpdateHouseholdCommand(Guid HouseholdId, Guid UserId, string Name) : IRequest<HouseholdDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record UpdateHouseholdBody(string Name);

public class UpdateHouseholdValidator : AbstractValidator<UpdateHouseholdBody>
{
    public UpdateHouseholdValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpdateHouseholdHandler(AppDbContext db)
    : IRequestHandler<UpdateHouseholdCommand, HouseholdDto>
{
    public async Task<HouseholdDto> Handle(
        UpdateHouseholdCommand request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireOwnerAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var household = await db.Households
            .FirstOrDefaultAsync(h => h.Id == request.HouseholdId, cancellationToken)
            ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        household.Name = request.Name;
        await db.SaveChangesAsync(cancellationToken);

        // Re-project with members for the response
        return await db.Households
            .Where(h => h.Id == request.HouseholdId)
            .Select(h => new HouseholdDto(
                h.Id,
                h.Name,
                h.OwnedByUserId,
                h.Owner.DisplayName,
                h.CreatedAt,
                h.Members.Select(m => new HouseholdMemberDto(
                    m.UserId, m.User.DisplayName, m.User.Email, m.Role, m.JoinedAt)).ToList(),
                h.WeatherStationIntegrationId.HasValue))
            .FirstAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpdateHouseholdEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/households/{householdId:guid}", async (
            Guid householdId,
            UpdateHouseholdBody body,
            IValidator<UpdateHouseholdBody> validator,
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
                var result = await mediator.Send(new UpdateHouseholdCommand(householdId, userId, body.Name), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("UpdateHousehold");
    }
}
