using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Features.Households;

public record HouseholdDto(
    Guid Id,
    string Name,
    Guid OwnedByUserId,
    string OwnerDisplayName,
    DateTime CreatedAt,
    IReadOnlyList<HouseholdMemberDto> Members,
    bool HasWeatherStation);

public record HouseholdMemberDto(
    Guid UserId,
    string DisplayName,
    string Email,
    HouseholdRole Role,
    DateTime JoinedAt);

public record WeatherStationDto(
    Guid Id,
    WeatherProvider Provider,
    string? StationId,
    bool HasApiKey,
    DateTime CreatedAt);
