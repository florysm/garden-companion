namespace GardenCompanion.Api.Tests.Features.Gardens;

public class GetGardenEndpointTests
{
    [Fact]
    public async Task Get_Garden_ReturnsUnauthorizedWithoutATestIdentity()
    {
        await using var factory = new TestApiFactory();
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/gardens/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Garden_ReturnsGardenDetailsForAMember()
    {
        await using var factory = new TestApiFactory();
        await factory.InitializeAsync();

        var user = TestDataFactory.CreateUser(displayName: "Garden Owner");
        var household = TestDataFactory.CreateHousehold(user);
        var householdMember = TestDataFactory.CreateHouseholdMember(household, user, HouseholdRole.Owner);
        var garden = TestDataFactory.CreateGarden(household, "Kitchen Garden");
        var gardenMember = TestDataFactory.CreateGardenMember(garden, user, GardenRole.Owner);
        var bed = TestDataFactory.CreateGardenBed(garden, "West Bed");

        await factory.SeedAsync(user, household, householdMember, garden, gardenMember, bed);

        var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.GetAsync($"/api/gardens/{garden.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GardenDetailDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(garden.Id);
        payload.UserRole.Should().Be(GardenRole.Owner.ToString());
        payload.Beds.Should().ContainSingle(b => b.Id == bed.Id && b.Name == "West Bed");
    }
}
