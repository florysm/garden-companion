namespace GardenCompanion.Api.Tests.Infrastructure;

public static class TestDataFactory
{
    public static User CreateUser(string? email = null, string? displayName = null)
    {
        var id = Guid.NewGuid();
        var resolvedDisplayName = displayName ?? $"Gardener {id.ToString("N")[..6]}";

        return new User
        {
            Id = id,
            Email = email ?? $"{id:N}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Passw0rd!"),
            DisplayName = resolvedDisplayName,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static UserSettings CreateUserSettings(User user) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };

    public static Household CreateHousehold(User owner, string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"{owner.DisplayName}'s Household",
            OwnedByUserId = owner.Id,
            CreatedAt = DateTime.UtcNow
        };

    public static HouseholdMember CreateHouseholdMember(
        Household household,
        User user,
        HouseholdRole role = HouseholdRole.Contributor) =>
        new()
        {
            Id = Guid.NewGuid(),
            HouseholdId = household.Id,
            UserId = user.Id,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

    public static Garden CreateGarden(Household household, string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            HouseholdId = household.Id,
            Name = name ?? "Backyard Garden",
            CreatedAt = DateTime.UtcNow
        };

    public static GardenMember CreateGardenMember(
        Garden garden,
        User user,
        GardenRole role = GardenRole.Contributor,
        Guid? invitedByUserId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            GardenId = garden.Id,
            UserId = user.Id,
            Role = role,
            InvitedByUserId = invitedByUserId
        };

    public static GardenBed CreateGardenBed(Garden garden, string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            GardenId = garden.Id,
            Name = name ?? "North Bed",
            Type = GardenBedType.RaisedGround,
            Shape = GardenBedShape.Rectangle,
            LengthFeet = 8,
            WidthFeet = 4,
            DepthInches = 12,
            SunExposure = SunExposure.FullSun
        };

    public static Plant CreatePlant(
        Guid? contributedByUserId = null,
        bool isGlobal = true,
        bool isApproved = true,
        string? commonName = null,
        int? daysToMaturity = 75) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalId = contributedByUserId.HasValue ? null : Guid.NewGuid().ToString("N"),
            ExternalSource = contributedByUserId.HasValue ? ExternalSource.Manual : ExternalSource.Perenual,
            ContributedByUserId = contributedByUserId,
            IsGlobal = isGlobal,
            IsApproved = isApproved,
            CommonName = commonName ?? "Tomato",
            ScientificName = "Solanum lycopersicum",
            Description = "A productive slicing tomato.",
            DaysToMaturity = daysToMaturity,
            MinSpacingInches = 24,
            MinDepthInches = 12,
            SunRequirement = "Full sun",
            WaterRequirement = "Even moisture",
            Family = "Nightshade",
            CachedAt = contributedByUserId.HasValue ? null : DateTime.UtcNow
        };

    public static UserRefreshToken CreateRefreshToken(
        User user,
        string token,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            RevokedAt = revokedAt,
            CreatedAt = DateTime.UtcNow
        };
}
