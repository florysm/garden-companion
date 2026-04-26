using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;

namespace GardenCompanion.Api.Features.Plants;

// ── Request / Response ───────────────────────────────────────────────────────

public record CreatePlantCommand(
    Guid UserId,
    string CommonName,
    string? ScientificName,
    string? Description,
    string? Family,
    int? DaysToMaturity,
    int? HeatLevelShu,
    decimal? MinSpacingInches,
    decimal? MinDepthInches,
    string? SunRequirement,
    string? WaterRequirement) : IRequest<PlantDetailDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
// Validated at endpoint level via a separate body record to exclude UserId.

public record CreatePlantBody(
    string CommonName,
    string? ScientificName,
    string? Description,
    string? Family,
    int? DaysToMaturity,
    int? HeatLevelShu,
    decimal? MinSpacingInches,
    decimal? MinDepthInches,
    string? SunRequirement,
    string? WaterRequirement);

public class CreatePlantValidator : AbstractValidator<CreatePlantBody>
{
    public CreatePlantValidator()
    {
        RuleFor(x => x.CommonName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ScientificName).MaximumLength(200).When(x => x.ScientificName is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Family).MaximumLength(100).When(x => x.Family is not null);
        RuleFor(x => x.DaysToMaturity).GreaterThan(0).When(x => x.DaysToMaturity.HasValue);
        RuleFor(x => x.MinSpacingInches).GreaterThan(0).When(x => x.MinSpacingInches.HasValue);
        RuleFor(x => x.MinDepthInches).GreaterThan(0).When(x => x.MinDepthInches.HasValue);
        RuleFor(x => x.SunRequirement).MaximumLength(100).When(x => x.SunRequirement is not null);
        RuleFor(x => x.WaterRequirement).MaximumLength(100).When(x => x.WaterRequirement is not null);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class CreatePlantHandler(AppDbContext db)
    : IRequestHandler<CreatePlantCommand, PlantDetailDto>
{
    public async Task<PlantDetailDto> Handle(
        CreatePlantCommand request, CancellationToken cancellationToken)
    {
        var plant = new Plant
        {
            Id = Guid.NewGuid(),
            CommonName = request.CommonName,
            ScientificName = request.ScientificName,
            Description = request.Description,
            Family = request.Family,
            DaysToMaturity = request.DaysToMaturity,
            HeatLevelShu = request.HeatLevelShu,
            MinSpacingInches = request.MinSpacingInches,
            MinDepthInches = request.MinDepthInches,
            SunRequirement = request.SunRequirement,
            WaterRequirement = request.WaterRequirement,
            ExternalSource = ExternalSource.Manual,
            ContributedByUserId = request.UserId,
            IsGlobal = false,
            IsApproved = false
        };

        db.Plants.Add(plant);
        await db.SaveChangesAsync(cancellationToken);

        return new PlantDetailDto(
            plant.Id,
            plant.CommonName,
            plant.ScientificName,
            plant.Description,
            plant.Family,
            plant.DaysToMaturity,
            plant.HeatLevelShu,
            plant.MinSpacingInches,
            plant.MinDepthInches,
            plant.SunRequirement,
            plant.WaterRequirement,
            plant.IsGlobal,
            plant.IsApproved,
            plant.ExternalSource,
            plant.ExternalId,
            plant.ContributedByUserId,
            plant.CachedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class CreatePlantEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/plants", async (
            CreatePlantBody body,
            IValidator<CreatePlantBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new CreatePlantCommand(
                userId,
                body.CommonName,
                body.ScientificName,
                body.Description,
                body.Family,
                body.DaysToMaturity,
                body.HeatLevelShu,
                body.MinSpacingInches,
                body.MinDepthInches,
                body.SunRequirement,
                body.WaterRequirement);

            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/plants/{result.Id}", result);
        })
        .RequireAuthorization()
        .WithTags("Plants")
        .WithName("CreatePlant");
    }
}
