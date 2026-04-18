using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Users;

// ── Request / Response ───────────────────────────────────────────────────────

public record UpdateMySettingsCommand(
    Guid UserId,
    decimal? LocationLatitude,
    decimal? LocationLongitude,
    string PreferredLanguage,
    TemperatureUnit TemperatureUnit,
    LengthUnit LengthUnit,
    WeightUnit WeightUnit,
    VolumeUnit VolumeUnit,
    string? UsdaHardinessZone,
    DateOnly? AverageFrostDateSpring,
    DateOnly? AverageFrostDateFall,
    bool ShareWeatherData,
    bool SharePlantingData,
    bool ShareHarvestData) : IRequest<UserSettingsDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record UpdateMySettingsBody(
    decimal? LocationLatitude,
    decimal? LocationLongitude,
    string PreferredLanguage,
    TemperatureUnit TemperatureUnit,
    LengthUnit LengthUnit,
    WeightUnit WeightUnit,
    VolumeUnit VolumeUnit,
    string? UsdaHardinessZone,
    DateOnly? AverageFrostDateSpring,
    DateOnly? AverageFrostDateFall,
    bool ShareWeatherData,
    bool SharePlantingData,
    bool ShareHarvestData);

public class UpdateMySettingsValidator : AbstractValidator<UpdateMySettingsBody>
{
    public UpdateMySettingsValidator()
    {
        RuleFor(x => x.LocationLatitude).InclusiveBetween(-90, 90).When(x => x.LocationLatitude.HasValue);
        RuleFor(x => x.LocationLongitude).InclusiveBetween(-180, 180).When(x => x.LocationLongitude.HasValue);
        RuleFor(x => x.PreferredLanguage).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TemperatureUnit).IsInEnum();
        RuleFor(x => x.LengthUnit).IsInEnum();
        RuleFor(x => x.WeightUnit).IsInEnum();
        RuleFor(x => x.VolumeUnit).IsInEnum();
        RuleFor(x => x.UsdaHardinessZone).MaximumLength(10).When(x => x.UsdaHardinessZone is not null);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpdateMySettingsHandler(AppDbContext db)
    : IRequestHandler<UpdateMySettingsCommand, UserSettingsDto>
{
    public async Task<UserSettingsDto> Handle(
        UpdateMySettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Settings not found for user {request.UserId}.");

        settings.LocationLatitude = request.LocationLatitude;
        settings.LocationLongitude = request.LocationLongitude;
        settings.PreferredLanguage = request.PreferredLanguage;
        settings.TemperatureUnit = request.TemperatureUnit;
        settings.LengthUnit = request.LengthUnit;
        settings.WeightUnit = request.WeightUnit;
        settings.VolumeUnit = request.VolumeUnit;
        settings.UsdaHardinessZone = request.UsdaHardinessZone;
        settings.AverageFrostDateSpring = request.AverageFrostDateSpring;
        settings.AverageFrostDateFall = request.AverageFrostDateFall;
        settings.ShareWeatherData = request.ShareWeatherData;
        settings.SharePlantingData = request.SharePlantingData;
        settings.ShareHarvestData = request.ShareHarvestData;

        await db.SaveChangesAsync(cancellationToken);

        return new UserSettingsDto(
            settings.Id,
            settings.LocationLatitude,
            settings.LocationLongitude,
            settings.PreferredLanguage,
            settings.TemperatureUnit,
            settings.LengthUnit,
            settings.WeightUnit,
            settings.VolumeUnit,
            settings.UsdaHardinessZone,
            settings.AverageFrostDateSpring,
            settings.AverageFrostDateFall,
            settings.ShareWeatherData,
            settings.SharePlantingData,
            settings.ShareHarvestData);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpdateMySettingsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/me/settings", async (
            UpdateMySettingsBody body,
            IValidator<UpdateMySettingsBody> validator,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new UpdateMySettingsCommand(
                userId,
                body.LocationLatitude,
                body.LocationLongitude,
                body.PreferredLanguage,
                body.TemperatureUnit,
                body.LengthUnit,
                body.WeightUnit,
                body.VolumeUnit,
                body.UsdaHardinessZone,
                body.AverageFrostDateSpring,
                body.AverageFrostDateFall,
                body.ShareWeatherData,
                body.SharePlantingData,
                body.ShareHarvestData);

            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Users")
        .WithName("UpdateMySettings");
    }
}
