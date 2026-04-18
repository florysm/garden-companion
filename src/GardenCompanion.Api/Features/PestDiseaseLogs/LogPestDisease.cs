using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using Severity = GardenCompanion.Api.Domain.Enums.Severity;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.PestDiseaseLogs;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record PestDiseaseLogDto(
    Guid Id,
    Guid GardenBedId,
    Guid? PlantingId,
    DateTime ObservedAt,
    PestDiseaseType Type,
    string Name,
    Severity Severity,
    string? TreatmentApplied,
    DateTime? ResolvedAt,
    string? Notes);

// ── Request / Response ───────────────────────────────────────────────────────

public record LogPestDiseaseCommand(
    Guid GardenId,
    Guid GardenBedId,
    Guid UserId,
    Guid? PlantingId,
    DateTime? ObservedAt,
    PestDiseaseType Type,
    string Name,
    Severity Severity,
    string? TreatmentApplied,
    string? Notes) : IRequest<PestDiseaseLogDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record LogPestDiseaseBody(
    Guid? PlantingId,
    DateTime? ObservedAt,
    PestDiseaseType Type,
    string Name,
    Severity Severity,
    string? TreatmentApplied,
    string? Notes);

public class LogPestDiseaseValidator : AbstractValidator<LogPestDiseaseBody>
{
    public LogPestDiseaseValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.TreatmentApplied).MaximumLength(500).When(x => x.TreatmentApplied is not null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class LogPestDiseaseHandler(AppDbContext db)
    : IRequestHandler<LogPestDiseaseCommand, PestDiseaseLogDto>
{
    public async Task<PestDiseaseLogDto> Handle(
        LogPestDiseaseCommand request, CancellationToken cancellationToken)
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

        var log = new PestDiseaseLog
        {
            Id = Guid.NewGuid(),
            GardenBedId = request.GardenBedId,
            PlantingId = request.PlantingId,
            ObservedAt = request.ObservedAt ?? DateTime.UtcNow,
            Type = request.Type,
            Name = request.Name,
            Severity = request.Severity,
            TreatmentApplied = request.TreatmentApplied,
            Notes = request.Notes
        };

        db.PestDiseaseLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        return new PestDiseaseLogDto(
            log.Id,
            log.GardenBedId,
            log.PlantingId,
            log.ObservedAt,
            log.Type,
            log.Name,
            log.Severity,
            log.TreatmentApplied,
            log.ResolvedAt,
            log.Notes);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class LogPestDiseaseEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/gardens/{gardenId:guid}/beds/{bedId:guid}/pest-disease-logs", async (
            Guid gardenId,
            Guid bedId,
            LogPestDiseaseBody body,
            IValidator<LogPestDiseaseBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new LogPestDiseaseCommand(
                gardenId, bedId, userId, body.PlantingId, body.ObservedAt,
                body.Type, body.Name, body.Severity, body.TreatmentApplied, body.Notes);

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/gardens/{gardenId}/beds/{bedId}/pest-disease-logs/{result.Id}", result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("PestDiseaseLogs")
        .WithName("LogPestDisease");
    }
}
