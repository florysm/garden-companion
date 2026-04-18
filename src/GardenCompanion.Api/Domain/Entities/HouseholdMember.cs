using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class HouseholdMember
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public HouseholdRole Role { get; set; } = HouseholdRole.Contributor;
    public DateTime JoinedAt { get; set; }

    // Navigation
    public Household Household { get; set; } = null!;
    public User User { get; set; } = null!;
}
