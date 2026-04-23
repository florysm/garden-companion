using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plants;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record PlantCompanionDto(
    Guid PlantId,
    Guid CompanionPlantId,
    string CompanionCommonName,
    string? CompanionScientificName,
    CompanionRelationshipType RelationshipType);

// ── Get ───────────────────────────────────────────────────────────────────────

public record GetPlantCompanionsQuery(Guid PlantId, Guid UserId)
    : IRequest<List<PlantCompanionDto>>;

public class GetPlantCompanionsHandler(AppDbContext db)
    : IRequestHandler<GetPlantCompanionsQuery, List<PlantCompanionDto>>
{
    public async Task<List<PlantCompanionDto>> Handle(
        GetPlantCompanionsQuery request, CancellationToken cancellationToken)
    {
        var plantVisible = await db.Plants.AnyAsync(
            p => p.Id == request.PlantId &&
                 ((p.IsGlobal && p.IsApproved) || p.ContributedByUserId == request.UserId),
            cancellationToken);

        if (!plantVisible)
            throw new KeyNotFoundException($"Plant {request.PlantId} not found.");

        return await db.PlantCompanions
            .Where(pc => pc.PlantId == request.PlantId &&
                         ((pc.CompanionPlant.IsGlobal && pc.CompanionPlant.IsApproved) ||
                          pc.CompanionPlant.ContributedByUserId == request.UserId))
            .Select(pc => new PlantCompanionDto(
                pc.PlantId,
                pc.CompanionPlantId,
                pc.CompanionPlant.CommonName,
                pc.CompanionPlant.ScientificName,
                pc.RelationshipType))
            .ToListAsync(cancellationToken);
    }
}

public static class GetPlantCompanionsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/plants/{id:guid}/companions", async (
            Guid id,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            try
            {
                var userId = ctx.User.GetUserId();
                var result = await mediator.Send(new GetPlantCompanionsQuery(id, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("Plants")
        .WithName("GetPlantCompanions");
    }
}

// ── Add ───────────────────────────────────────────────────────────────────────

public record AddPlantCompanionCommand(Guid PlantId, Guid UserId, Guid CompanionPlantId, CompanionRelationshipType RelationshipType)
    : IRequest<PlantCompanionDto>;

public class AddPlantCompanionHandler(AppDbContext db)
    : IRequestHandler<AddPlantCompanionCommand, PlantCompanionDto>
{
    public async Task<PlantCompanionDto> Handle(
        AddPlantCompanionCommand request, CancellationToken cancellationToken)
    {
        var plant = await db.Plants
            .Where(p => p.Id == request.PlantId)
            .Select(p => new { p.ContributedByUserId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Plant {request.PlantId} not found.");

        if (plant.ContributedByUserId != request.UserId)
            throw new UnauthorizedAccessException("Only the plant contributor can manage companion relationships.");

        var companionVisible = await db.Plants.AnyAsync(
            p => p.Id == request.CompanionPlantId &&
                 ((p.IsGlobal && p.IsApproved) || p.ContributedByUserId == request.UserId),
            cancellationToken);

        if (!companionVisible)
            throw new KeyNotFoundException($"Plant {request.CompanionPlantId} not found.");

        var alreadyExists = await db.PlantCompanions.AnyAsync(
            pc => pc.PlantId == request.PlantId && pc.CompanionPlantId == request.CompanionPlantId,
            cancellationToken);

        if (alreadyExists)
            throw new InvalidOperationException("Companion relationship already exists.");

        var companion = new PlantCompanion
        {
            PlantId = request.PlantId,
            CompanionPlantId = request.CompanionPlantId,
            RelationshipType = request.RelationshipType
        };
        db.PlantCompanions.Add(companion);
        await db.SaveChangesAsync(cancellationToken);

        var companionName = await db.Plants
            .Where(p => p.Id == request.CompanionPlantId)
            .Select(p => new { p.CommonName, p.ScientificName })
            .FirstAsync(cancellationToken);

        return new PlantCompanionDto(
            companion.PlantId,
            companion.CompanionPlantId,
            companionName.CommonName,
            companionName.ScientificName,
            companion.RelationshipType);
    }
}

public static class AddPlantCompanionEndpoint
{
    public record Request(Guid CompanionPlantId, CompanionRelationshipType RelationshipType);

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/plants/{id:guid}/companions", async (
            Guid id,
            Request req,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            try
            {
                var userId = ctx.User.GetUserId();
                var result = await mediator.Send(
                    new AddPlantCompanionCommand(id, userId, req.CompanionPlantId, req.RelationshipType), ct);
                return Results.Created($"/api/plants/{id}/companions", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("Plants")
        .WithName("AddPlantCompanion");
    }
}

// ── Remove ────────────────────────────────────────────────────────────────────

public record RemovePlantCompanionCommand(Guid PlantId, Guid UserId, Guid CompanionPlantId)
    : IRequest;

public class RemovePlantCompanionHandler(AppDbContext db)
    : IRequestHandler<RemovePlantCompanionCommand>
{
    public async Task Handle(
        RemovePlantCompanionCommand request, CancellationToken cancellationToken)
    {
        var plant = await db.Plants
            .Where(p => p.Id == request.PlantId)
            .Select(p => new { p.ContributedByUserId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Plant {request.PlantId} not found.");

        if (plant.ContributedByUserId != request.UserId)
            throw new UnauthorizedAccessException("Only the plant contributor can manage companion relationships.");

        var companion = await db.PlantCompanions
            .FirstOrDefaultAsync(
                pc => pc.PlantId == request.PlantId && pc.CompanionPlantId == request.CompanionPlantId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Companion relationship not found.");

        db.PlantCompanions.Remove(companion);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public static class RemovePlantCompanionEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/plants/{id:guid}/companions/{companionPlantId:guid}", async (
            Guid id,
            Guid companionPlantId,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            try
            {
                var userId = ctx.User.GetUserId();
                await mediator.Send(new RemovePlantCompanionCommand(id, userId, companionPlantId), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden); }
        })
        .RequireAuthorization()
        .WithTags("Plants")
        .WithName("RemovePlantCompanion");
    }
}
