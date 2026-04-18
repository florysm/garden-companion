using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenBeds;

// ── Request / Response ──────────────────────────────────────────────────────

public record UpdateGardenBedCommand(
    Guid CurrentUserId,
    Guid GardenId,
    Guid BedId,
    string Name,
    GardenBedType Type,
    GardenBedShape Shape,
    decimal? LengthFeet,
    decimal? WidthFeet,
    decimal? DiameterFeet,
    decimal? DepthInches,
    decimal? VolumeGallons,
    string? SoilType,
    SunExposure SunExposure,
    string? Notes) : IRequest<GardenBedDetailDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public class UpdateGardenBedValidator : AbstractValidator<UpdateGardenBedCommand>
{
    public UpdateGardenBedValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SoilType).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.LengthFeet).GreaterThan(0).When(x => x.LengthFeet.HasValue);
        RuleFor(x => x.WidthFeet).GreaterThan(0).When(x => x.WidthFeet.HasValue);
        RuleFor(x => x.DiameterFeet).GreaterThan(0).When(x => x.DiameterFeet.HasValue);
        RuleFor(x => x.DepthInches).GreaterThan(0).When(x => x.DepthInches.HasValue);
        RuleFor(x => x.VolumeGallons).GreaterThan(0).When(x => x.VolumeGallons.HasValue);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpdateGardenBedHandler(AppDbContext db)
    : IRequestHandler<UpdateGardenBedCommand, GardenBedDetailDto>
{
    public async Task<GardenBedDetailDto> Handle(
        UpdateGardenBedCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var bed = await db.GardenBeds
            .Include(b => b.Plantings)
            .FirstOrDefaultAsync(
                b => b.Id == request.BedId && b.GardenId == request.GardenId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Bed {request.BedId} not found.");

        bed.Name = request.Name;
        bed.Type = request.Type;
        bed.Shape = request.Shape;
        bed.LengthFeet = request.LengthFeet;
        bed.WidthFeet = request.WidthFeet;
        bed.DiameterFeet = request.DiameterFeet;
        bed.DepthInches = request.DepthInches;
        bed.VolumeGallons = request.VolumeGallons;
        bed.SoilType = request.SoilType;
        bed.SunExposure = request.SunExposure;
        bed.Notes = request.Notes;

        await db.SaveChangesAsync(cancellationToken);

        return CreateGardenBedHandler.ToDto(bed, bed.Plantings.Count(p => p.IsActive));
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpdateGardenBedEndpoint
{
    public record Request(
        string Name,
        string Type,
        string Shape,
        decimal? LengthFeet,
        decimal? WidthFeet,
        decimal? DiameterFeet,
        decimal? DepthInches,
        decimal? VolumeGallons,
        string? SoilType,
        string SunExposure,
        string? Notes);

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/gardens/{gardenId:guid}/beds/{bedId:guid}", async (
            Guid gardenId,
            Guid bedId,
            Request req,
            HttpContext ctx,
            IValidator<UpdateGardenBedCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();

            if (!Enum.TryParse<GardenBedType>(req.Type, true, out var type))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["type"] = [$"Invalid bed type '{req.Type}'."] });
            if (!Enum.TryParse<GardenBedShape>(req.Shape, true, out var shape))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["shape"] = [$"Invalid shape '{req.Shape}'."] });
            if (!Enum.TryParse<SunExposure>(req.SunExposure, true, out var sun))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["sunExposure"] = [$"Invalid sun exposure '{req.SunExposure}'."] });

            var command = new UpdateGardenBedCommand(
                userId, gardenId, bedId, req.Name, type, shape,
                req.LengthFeet, req.WidthFeet, req.DiameterFeet,
                req.DepthInches, req.VolumeGallons, req.SoilType, sun, req.Notes);

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
        .WithTags("GardenBeds")
        .WithName("UpdateGardenBed");
    }
}
