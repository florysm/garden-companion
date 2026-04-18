using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;

namespace GardenCompanion.Api.Features.SoilTests;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record SoilTestDto(
    Guid Id,
    Guid GardenBedId,
    DateOnly TestedAt,
    decimal? PhLevel,
    decimal? NitrogenPpm,
    decimal? PhosphorusPpm,
    decimal? PotassiumPpm,
    decimal? OrganicMatterPercent,
    string TestSource,
    string? Notes,
    DateTime CreatedAt);

// ── Request ──────────────────────────────────────────────────────────────────

public record CreateSoilTestCommand(
    Guid CurrentUserId,
    Guid GardenId,
    Guid BedId,
    DateOnly TestedAt,
    decimal? PhLevel,
    decimal? NitrogenPpm,
    decimal? PhosphorusPpm,
    decimal? PotassiumPpm,
    decimal? OrganicMatterPercent,
    SoilTestSource TestSource,
    string? Notes) : IRequest<SoilTestDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public class CreateSoilTestValidator : AbstractValidator<CreateSoilTestCommand>
{
    public CreateSoilTestValidator()
    {
        RuleFor(x => x.TestedAt).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Test date cannot be in the future.");
        RuleFor(x => x.PhLevel)
            .InclusiveBetween(0m, 14m).When(x => x.PhLevel.HasValue);
        RuleFor(x => x.NitrogenPpm).GreaterThanOrEqualTo(0).When(x => x.NitrogenPpm.HasValue);
        RuleFor(x => x.PhosphorusPpm).GreaterThanOrEqualTo(0).When(x => x.PhosphorusPpm.HasValue);
        RuleFor(x => x.PotassiumPpm).GreaterThanOrEqualTo(0).When(x => x.PotassiumPpm.HasValue);
        RuleFor(x => x.OrganicMatterPercent)
            .InclusiveBetween(0m, 100m).When(x => x.OrganicMatterPercent.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class CreateSoilTestHandler(AppDbContext db)
    : IRequestHandler<CreateSoilTestCommand, SoilTestDto>
{
    public async Task<SoilTestDto> Handle(
        CreateSoilTestCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireBedMemberAsync(
            db, request.BedId, request.CurrentUserId, cancellationToken);

        var test = new SoilTest
        {
            Id = Guid.NewGuid(),
            GardenBedId = request.BedId,
            TestedAt = request.TestedAt,
            PhLevel = request.PhLevel,
            NitrogenPpm = request.NitrogenPpm,
            PhosphorusPpm = request.PhosphorusPpm,
            PotassiumPpm = request.PotassiumPpm,
            OrganicMatterPercent = request.OrganicMatterPercent,
            TestSource = request.TestSource,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        db.SoilTests.Add(test);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(test);
    }

    internal static SoilTestDto ToDto(SoilTest t) =>
        new(t.Id, t.GardenBedId, t.TestedAt, t.PhLevel, t.NitrogenPpm,
            t.PhosphorusPpm, t.PotassiumPpm, t.OrganicMatterPercent,
            t.TestSource.ToString(), t.Notes, t.CreatedAt);
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class CreateSoilTestEndpoint
{
    public record Request(
        DateOnly TestedAt,
        decimal? PhLevel,
        decimal? NitrogenPpm,
        decimal? PhosphorusPpm,
        decimal? PotassiumPpm,
        decimal? OrganicMatterPercent,
        string TestSource,
        string? Notes);

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/gardens/{gardenId:guid}/beds/{bedId:guid}/soil-tests", async (
            Guid gardenId,
            Guid bedId,
            Request req,
            HttpContext ctx,
            IValidator<CreateSoilTestCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();

            if (!Enum.TryParse<SoilTestSource>(req.TestSource, true, out var source))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["testSource"] = [$"Invalid test source '{req.TestSource}'."] });

            var command = new CreateSoilTestCommand(
                userId, gardenId, bedId, req.TestedAt,
                req.PhLevel, req.NitrogenPpm, req.PhosphorusPpm,
                req.PotassiumPpm, req.OrganicMatterPercent, source, req.Notes);

            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created(
                    $"/api/gardens/{gardenId}/beds/{bedId}/soil-tests/{result.Id}", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("SoilTests")
        .WithName("CreateSoilTest");
    }
}
