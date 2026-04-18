using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Households;

// ── Request / Response ───────────────────────────────────────────────────────

public record UpsertWeatherStationCommand(
    Guid HouseholdId,
    Guid UserId,
    WeatherProvider Provider,
    string? StationId,
    string? ApiKey) : IRequest<WeatherStationDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record UpsertWeatherStationBody(
    WeatherProvider Provider,
    string? StationId,
    string? ApiKey);

public class UpsertWeatherStationValidator : AbstractValidator<UpsertWeatherStationBody>
{
    public UpsertWeatherStationValidator()
    {
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.StationId).MaximumLength(100).When(x => x.StationId is not null);
        // ApiKey intentionally not length-validated here; stored as-is (encrypt before persistence in production)
        RuleFor(x => x.ApiKey).MaximumLength(512).When(x => x.ApiKey is not null);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpsertWeatherStationHandler(AppDbContext db)
    : IRequestHandler<UpsertWeatherStationCommand, WeatherStationDto>
{
    public async Task<WeatherStationDto> Handle(
        UpsertWeatherStationCommand request, CancellationToken cancellationToken)
    {
        await HouseholdAccess.RequireOwnerAsync(db, request.HouseholdId, request.UserId, cancellationToken);

        var household = await db.Households
            .Include(h => h.WeatherStationIntegration)
            .FirstOrDefaultAsync(h => h.Id == request.HouseholdId, cancellationToken)
            ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        WeatherStationIntegration station;

        if (household.WeatherStationIntegration is not null)
        {
            // Update existing
            station = household.WeatherStationIntegration;
            station.Provider = request.Provider;
            station.StationId = request.StationId;
            if (request.ApiKey is not null)
                station.ApiKey = request.ApiKey;
        }
        else
        {
            // Create new — save station first to resolve the circular FK
            station = new WeatherStationIntegration
            {
                Id = Guid.NewGuid(),
                HouseholdId = request.HouseholdId,
                Provider = request.Provider,
                StationId = request.StationId,
                ApiKey = request.ApiKey,
                CreatedAt = DateTime.UtcNow
            };
            db.WeatherStationIntegrations.Add(station);
            await db.SaveChangesAsync(cancellationToken);

            household.WeatherStationIntegrationId = station.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new WeatherStationDto(
            station.Id,
            station.Provider,
            station.StationId,
            station.ApiKey is not null,
            station.CreatedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpsertWeatherStationEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/households/{householdId:guid}/weather-station", async (
            Guid householdId,
            UpsertWeatherStationBody body,
            IValidator<UpsertWeatherStationBody> validator,
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
                    new UpsertWeatherStationCommand(householdId, userId, body.Provider, body.StationId, body.ApiKey), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization()
        .WithTags("Households")
        .WithName("UpsertWeatherStation");
    }
}
