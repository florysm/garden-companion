namespace GardenCompanion.Api.Domain.Entities;

public class Garden
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Household Household { get; set; } = null!;
    public ICollection<GardenType> GardenTypes { get; set; } = [];
    public ICollection<GardenMember> Members { get; set; } = [];
    public ICollection<GardenBed> Beds { get; set; } = [];
    public ICollection<GardenTask> Tasks { get; set; } = [];
    public ICollection<UserInsight> Insights { get; set; } = [];
}
