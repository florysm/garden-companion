namespace GardenCompanion.Api.Features.Gardens;

// Shared DTOs used across multiple Garden feature slices.

public record GardenSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Types,
    int BedCount,
    string UserRole,
    DateTime CreatedAt);

public record GardenDetailDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Types,
    IReadOnlyList<GardenBedSummaryDto> Beds,
    IReadOnlyList<GardenMemberDto> Members,
    string UserRole,
    DateTime CreatedAt);

public record GardenBedSummaryDto(
    Guid Id,
    string Name,
    string Type,
    string Shape,
    string SunExposure,
    int ActivePlantingCount);

public record GardenMemberDto(
    Guid UserId,
    string DisplayName,
    string Role);
