namespace GardenCompanion.Api.Features.GardenTasks;

public record GardenTaskDto(
    Guid Id,
    Guid GardenId,
    Guid? GardenBedId,
    Guid? PlantingId,
    Guid? AssignedToUserId,
    string? AssignedToDisplayName,
    string Title,
    string? Description,
    string TaskType,
    DateOnly? DueDate,
    bool IsOverdue,
    DateTime? CompletedAt,
    DateTime CreatedAt);
