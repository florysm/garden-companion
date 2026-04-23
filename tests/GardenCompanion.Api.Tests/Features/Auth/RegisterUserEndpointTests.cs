namespace GardenCompanion.Api.Tests.Features.Auth;

public class RegisterUserEndpointTests
{
    [Fact]
    public async Task Post_Register_CreatesTheUserAndReturnsCreated()
    {
        await using var factory = new TestApiFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "api-gardener@example.com",
            password = "Passw0rd!",
            displayName = "API Gardener"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        payload.Should().NotBeNull();
        payload!.Email.Should().Be("api-gardener@example.com");

        var userCount = await factory.ExecuteDbContextAsync(db => db.Users.CountAsync());
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task Post_Register_ReturnsBadRequestForInvalidPayload()
    {
        await using var factory = new TestApiFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "short",
            displayName = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
