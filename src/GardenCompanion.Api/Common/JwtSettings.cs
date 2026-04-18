namespace GardenCompanion.Api.Common;

public class JwtSettings
{
    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = "GardenCompanion";
    public string Audience { get; init; } = "GardenCompanion";
    public int ExpiryMinutes { get; init; } = 15;
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
