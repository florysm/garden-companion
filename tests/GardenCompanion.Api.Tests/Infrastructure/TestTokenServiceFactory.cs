namespace GardenCompanion.Api.Tests.Infrastructure;

public static class TestTokenServiceFactory
{
    public static TokenService Create() =>
        new(Options.Create(new JwtSettings
        {
            Secret = "garden-companion-tests-secret-0123456789",
            Issuer = "GardenCompanion.Tests",
            Audience = "GardenCompanion.Tests",
            ExpiryMinutes = 30,
            RefreshTokenExpiryDays = 7
        }));
}
