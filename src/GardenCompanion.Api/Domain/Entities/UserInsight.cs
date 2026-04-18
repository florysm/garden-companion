using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class UserInsight
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid? GardenId { get; set; }
    public Guid? GardenBedId { get; set; }
    public InsightType InsightType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime GeneratedAt { get; set; }

    // Navigation
    public Household Household { get; set; } = null!;
    public Garden? Garden { get; set; }
    public GardenBed? GardenBed { get; set; }
}
