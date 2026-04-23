using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.UserInsights;

// ── Request / Response ───────────────────────────────────────────────────────

public record CreateUserInsightCommand(
    Guid HouseholdId,
    Guid UserId,
    Guid? GardenId,
    Guid? GardenBedId,
    InsightType InsightType,
    string Title,
    string Body,
    DateTime? ExpiresAt) : IRequest<UserInsightDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record CreateUserInsightBody(
    Guid? GardenId,
    Guid? GardenBedId,
    InsightType InsightType,
    string Title,
    string Body,
    DateTime? ExpiresAt);

public class CreateUserInsightValidator : AbstractValidator<CreateUserInsightBody>
{
    public CreateUserInsightValidator()
    {
        RuleFor(x => x.InsightType).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ExpiresAt).GreaterThan(_ => DateTime.UtcNow).When(x => x.ExpiresAt.HasValue);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class CreateUserInsightHandler(AppDbContext db)
    : IRequestHandler<CreateUserInsightCommand, UserInsightDto>
{
    public async Task<UserInsightDto> Handle(
        CreateUserInsightCommand request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireOwnerAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        if (request.GardenId.HasValue)
        {
            var gardenBelongsToHousehold = await db.Gardens
                .AnyAsync(g => g.Id == request.GardenId.Value && g.HouseholdId == request.HouseholdId, cancellationToken);
            if (!gardenBelongsToHousehold)
                throw new KeyNotFoundException($"Garden {request.GardenId} not found in household {request.HouseholdId}.");
        }

        if (request.GardenBedId.HasValue)
        {
            var bedBelongsToHousehold = await db.GardenBeds
                .AnyAsync(b => b.Id == request.GardenBedId.Value && b.Garden.HouseholdId == request.HouseholdId, cancellationToken);
            if (!bedBelongsToHousehold)
                throw new KeyNotFoundException($"Garden bed {request.GardenBedId} not found in household {request.HouseholdId}.");
        }

        var insight = new UserInsight
        {
            Id = Guid.NewGuid(),
            HouseholdId = request.HouseholdId,
            GardenId = request.GardenId,
            GardenBedId = request.GardenBedId,
            InsightType = request.InsightType,
            Title = request.Title,
            Body = request.Body,
            IsRead = false,
            ExpiresAt = request.ExpiresAt,
            GeneratedAt = DateTime.UtcNow
        };

        db.UserInsights.Add(insight);
        await db.SaveChangesAsync(cancellationToken);

        return new UserInsightDto(
            insight.Id, insight.HouseholdId, insight.GardenId, insight.GardenBedId,
            insight.InsightType, insight.Title, insight.Body, insight.IsRead,
            insight.ExpiresAt, insight.GeneratedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class CreateUserInsightEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/households/{householdId:guid}/insights", async (
            Guid householdId,
            CreateUserInsightBody body,
            IValidator<CreateUserInsightBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new CreateUserInsightCommand(
                householdId, userId, body.GardenId, body.GardenBedId,
                body.InsightType, body.Title, body.Body, body.ExpiresAt);

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/households/{householdId}/insights/{result.Id}", result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
        })
        .RequireAuthorization()
        .WithTags("UserInsights")
        .WithName("CreateUserInsight");
    }
}
