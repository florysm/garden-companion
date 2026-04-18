using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class GardenTask
{
    public Guid Id { get; set; }
    public Guid GardenId { get; set; }
    public Guid? GardenBedId { get; set; }
    public Guid? PlantingId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskType TaskType { get; set; } = TaskType.General;
    public DateOnly? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Garden Garden { get; set; } = null!;
    public GardenBed? GardenBed { get; set; }
    public Planting? Planting { get; set; }
    public User? AssignedTo { get; set; }
}
