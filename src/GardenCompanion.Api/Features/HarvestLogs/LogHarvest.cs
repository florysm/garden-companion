using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.HarvestLogs;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record HarvestLogDto(
    Guid Id,
    Guid PlantingId,
    Guid HarvestedByUserId,
    string HarvestedByDisplayName,
    DateOnly HarvestDate,
    decimal Quantity,
    QuantityUnit QuantityUnit,
    string? Notes,
    DateTime CreatedAt);

// ── Request / Response ───────────────────────────────────────────────────────

public record LogHarvestCommand(
    Guid PlantingId,
    Guid UserId,
    DateOnly HarvestDate,
    decimal Quantity,
    QuantityUnit QuantityUnit,
    string? Notes) : IRequest<HarvestLogDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record LogHarvestBody(
    DateOnly HarvestDate,
    decimal Quantity,
    QuantityUnit QuantityUnit,
    string? Notes);

public class LogHarvestValidator : AbstractValidator<LogHarvestBody>
{
    public LogHarvestValidator()
    {
        RuleFor(x => x.HarvestDate).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.QuantityUnit).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class LogHarvestHandler(AppDbContext db)
    : IRequestHandler<LogHarvestCommand, HarvestLogDto>
{
    public async Task<HarvestLogDto> Handle(
        LogHarvestCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequirePlantingMemberAsync(db, request.PlantingId, request.UserId, cancellationToken);

        var harvesterName = await db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Unknown";

        var now = DateTime.UtcNow;
        var log = new HarvestLog
        {
            Id = Guid.NewGuid(),
            PlantingId = request.PlantingId,
            HarvestedByUserId = request.UserId,
            HarvestDate = request.HarvestDate,
            Quantity = request.Quantity,
            QuantityUnit = request.QuantityUnit,
            Notes = request.Notes,
            CreatedAt = now
        };

        db.HarvestLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        return new HarvestLogDto(
            log.Id,
            log.PlantingId,
            log.HarvestedByUserId,
            harvesterName,
            log.HarvestDate,
            log.Quantity,
            log.QuantityUnit,
            log.Notes,
            log.CreatedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class LogHarvestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/plantings/{plantingId:guid}/harvests", async (
            Guid plantingId,
            LogHarvestBody body,
            IValidator<LogHarvestBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new LogHarvestCommand(plantingId, userId, body.HarvestDate, body.Quantity, body.QuantityUnit, body.Notes);

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/plantings/{plantingId}/harvests/{result.Id}", result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("HarvestLogs")
        .WithName("LogHarvest");
    }
}
