namespace GardenCompanion.Api.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public UserSettings? Settings { get; set; }
    public ICollection<HouseholdMember> HouseholdMemberships { get; set; } = [];
    public ICollection<Household> OwnedHouseholds { get; set; } = [];
    public ICollection<GardenMember> GardenMemberships { get; set; } = [];
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<HarvestLog> HarvestLogs { get; set; } = [];
}
