using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.AmendmentLogs;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record AmendmentLogDto(
    Guid Id,
    Guid GardenBedId,
    Guid? PlantingId,
    DateOnly AppliedAt,
    string ProductName,
    AmendmentType AmendmentType,
    decimal Quantity,
    QuantityUnit QuantityUnit,
    string? Notes);

// ── Request / Response ───────────────────────────────────────────────────────

public record LogAmendmentCommand(
    Guid GardenId,
    Guid GardenBedId,
    Guid UserId,
    Guid? PlantingId,
    DateOnly AppliedAt,
    string ProductName,
    AmendmentType AmendmentType,
    decimal Quantity,
    QuantityUnit QuantityUnit,
    string? Notes) : IRequest<AmendmentLogDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record LogAmendmentBody(
    Guid? PlantingId,
    DateOnly AppliedAt,
    string ProductName,
    AmendmentType AmendmentType,
    decimal Quantity,
    QuantityUnit QuantityUnit,
    string? Notes);

public class LogAmendmentValidator : AbstractValidator<LogAmendmentBody>
{
    public LogAmendmentValidator()
    {
        RuleFor(x => x.AppliedAt).NotEmpty();
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AmendmentType).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.QuantityUnit).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class LogAmendmentHandler(AppDbContext db)
    : IRequestHandler<LogAmendmentCommand, AmendmentLogDto>
{
    public async Task<AmendmentLogDto> Handle(
        LogAmendmentCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(db, request.GardenId, request.UserId, cancellationToken);

        var bedExists = await db.GardenBeds
            .AnyAsync(b => b.Id == request.GardenBedId && b.GardenId == request.GardenId, cancellationToken);
        if (!bedExists)
            throw new KeyNotFoundException($"Garden bed {request.GardenBedId} not found in garden {request.GardenId}.");

        if (request.PlantingId.HasValue)
        {
            var plantingExists = await db.Plantings
                .AnyAsync(p => p.Id == request.PlantingId.Value && p.GardenBedId == request.GardenBedId, cancellationToken);
            if (!plantingExists)
                throw new KeyNotFoundException($"Planting {request.PlantingId} not found in bed {request.GardenBedId}.");
        }

        var log = new AmendmentLog
        {
            Id = Guid.NewGuid(),
            GardenBedId = request.GardenBedId,
            PlantingId = request.PlantingId,
            AppliedAt = request.AppliedAt,
            ProductName = request.ProductName,
            AmendmentType = request.AmendmentType,
            Quantity = request.Quantity,
            QuantityUnit = request.QuantityUnit,
            Notes = request.Notes
        };

        db.AmendmentLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        return new AmendmentLogDto(
            log.Id,
            log.GardenBedId,
            log.PlantingId,
            log.AppliedAt,
            log.ProductName,
            log.AmendmentType,
            log.Quantity,
            log.QuantityUnit,
            log.Notes);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class LogAmendmentEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/gardens/{gardenId:guid}/beds/{bedId:guid}/amendments", async (
            Guid gardenId,
            Guid bedId,
            LogAmendmentBody body,
            IValidator<LogAmendmentBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new LogAmendmentCommand(
                gardenId, bedId, userId, body.PlantingId, body.AppliedAt,
                body.ProductName, body.AmendmentType, body.Quantity, body.QuantityUnit, body.Notes);

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/gardens/{gardenId}/beds/{bedId}/amendments/{result.Id}", result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("AmendmentLogs")
        .WithName("LogAmendment");
    }
}
