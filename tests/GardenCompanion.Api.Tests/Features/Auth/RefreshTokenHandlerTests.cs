namespace GardenCompanion.Api.Tests.Features.Auth;

public class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_RotatesRefreshTokenAndRevokesTheOldToken()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        const string plainOldToken = "old-refresh-token";
        var user = TestDataFactory.CreateUser();
        var existingToken = TestDataFactory.CreateRefreshToken(user, TokenService.HashToken(plainOldToken));

        db.AddRange(user, existingToken);
        await db.SaveChangesAsync();

        var handler = new RefreshTokenHandler(db, TestTokenServiceFactory.Create());

        var response = await handler.Handle(
            new RefreshTokenCommand(plainOldToken),
            CancellationToken.None);

        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var storedTokens = await db.UserRefreshTokens
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        storedTokens.Should().HaveCount(2);
        storedTokens.Single(t => t.Id == existingToken.Id).RevokedAt.Should().NotBeNull();
        storedTokens.Single(t => t.Id != existingToken.Id).Token.Should().Be(TokenService.HashToken(response.RefreshToken));
    }

    [Fact]
    public async Task Handle_Throws_WhenRefreshTokenIsUnknown()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var handler = new RefreshTokenHandler(db, TestTokenServiceFactory.Create());

        var act = () => handler.Handle(new RefreshTokenCommand("missing-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*invalid or expired*");
    }
}
