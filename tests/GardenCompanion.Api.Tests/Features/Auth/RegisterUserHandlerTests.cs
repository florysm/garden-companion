namespace GardenCompanion.Api.Tests.Features.Auth;

public class RegisterUserHandlerTests
{
    [Fact]
    public async Task Handle_CreatesUserSettingsHouseholdMembershipAndRefreshToken()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var handler = new RegisterUserHandler(db, TestTokenServiceFactory.Create());

        var response = await handler.Handle(
            new RegisterUserCommand("gardener@example.com", "Passw0rd!", "Stephen"),
            CancellationToken.None);

        response.Email.Should().Be("gardener@example.com");
        response.DisplayName.Should().Be("Stephen");
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var user = await db.Users.SingleAsync();
        user.Email.Should().Be("gardener@example.com");

        var settings = await db.UserSettings.SingleAsync();
        settings.UserId.Should().Be(user.Id);

        var household = await db.Households.SingleAsync();
        household.OwnedByUserId.Should().Be(user.Id);
        household.Name.Should().Be("Stephen's Garden");

        var membership = await db.HouseholdMembers.SingleAsync();
        membership.UserId.Should().Be(user.Id);
        membership.HouseholdId.Should().Be(household.Id);
        membership.Role.Should().Be(HouseholdRole.Owner);

        var refreshToken = await db.UserRefreshTokens.SingleAsync();
        refreshToken.UserId.Should().Be(user.Id);
        refreshToken.Token.Should().Be(response.RefreshToken);
    }

    [Fact]
    public async Task Handle_Throws_WhenEmailAlreadyExistsCaseInsensitively()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var existingUser = TestDataFactory.CreateUser(email: "gardener@example.com");
        db.Users.Add(existingUser);
        await db.SaveChangesAsync();

        var handler = new RegisterUserHandler(db, TestTokenServiceFactory.Create());

        var act = () => handler.Handle(
            new RegisterUserCommand("Gardener@Example.com", "Passw0rd!", "Stephen"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }
}
