using GardenCompanion.Api.Features.Households;
using GardenCompanion.Api.Infrastructure.ExternalData.Weather;

namespace GardenCompanion.Api.Tests.Features.Households;

public class TestWeatherStationTests
{
    [Fact]
    public async Task Handle_UsesSavedApiKeyWhenRequestOmitsItAndDoesNotPersistObservation()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var user = TestDataFactory.CreateUser(displayName: "Station Owner");
        var household = TestDataFactory.CreateHousehold(user);
        var member = TestDataFactory.CreateHouseholdMember(household, user, HouseholdRole.Owner);
        db.AddRange(user, household, member);
        await db.SaveChangesAsync();

        var station = new WeatherStationIntegration
        {
            Id = Guid.NewGuid(),
            HouseholdId = household.Id,
            Provider = WeatherProvider.WeatherUnderground,
            StationId = "saved-station",
            ApiKey = "saved-key",
            CreatedAt = DateTime.UtcNow,
        };
        db.WeatherStationIntegrations.Add(station);
        await db.SaveChangesAsync();

        household.WeatherStationIntegrationId = station.Id;
        await db.SaveChangesAsync();

        var provider = new FakeWeatherProvider(
            WeatherProvider.WeatherUnderground,
            new WeatherObservationData(
                DateTime.UtcNow,
                72.4m,
                65.2m,
                5.1m,
                180,
                0.01m,
                1.2m,
                4.3m,
                60.1m,
                29.92m,
                "live-station"));
        var handler = new TestWeatherStationHandler(db, [provider]);

        var result = await handler.Handle(
            new TestWeatherStationCommand(
                household.Id,
                user.Id,
                WeatherProvider.WeatherUnderground,
                "request-station",
                ApiKey: null),
            CancellationToken.None);

        result.TemperatureF.Should().Be(72.4m);
        result.StationId.Should().Be("live-station");
        provider.LastStation.Should().NotBeNull();
        provider.LastStation!.StationId.Should().Be("request-station");
        provider.LastStation.ApiKey.Should().Be("saved-key");
        provider.LastHousehold.Should().NotBeNull();
        provider.LastHousehold!.Id.Should().Be(household.Id);

        (await db.WeatherObservations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_ThrowsUnprocessablePathWhenProviderReturnsNoData()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var user = TestDataFactory.CreateUser(displayName: "Station Owner");
        var household = TestDataFactory.CreateHousehold(user);
        var member = TestDataFactory.CreateHouseholdMember(household, user, HouseholdRole.Owner);
        db.AddRange(user, household, member);
        await db.SaveChangesAsync();

        var handler = new TestWeatherStationHandler(
            db,
            [new FakeWeatherProvider(WeatherProvider.OpenMeteo, result: null)]);

        var act = () => handler.Handle(
            new TestWeatherStationCommand(
                household.Id,
                user.Id,
                WeatherProvider.OpenMeteo,
                StationId: null,
                ApiKey: null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Provider returned no data*");
    }

    [Fact]
    public async Task Post_ReturnsValidationProblemForInvalidBody()
    {
        await using var factory = new TestApiFactory();
        await factory.InitializeAsync();

        var client = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var response = await client.PostAsJsonAsync(
            $"/api/households/{Guid.NewGuid()}/weather-station/test",
            new
            {
                provider = "OpenMeteo",
                stationId = new string('x', 101),
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class FakeWeatherProvider(
        WeatherProvider providerType,
        WeatherObservationData? result) : IWeatherProvider
    {
        public WeatherProvider ProviderType { get; } = providerType;
        public WeatherStationIntegration? LastStation { get; private set; }
        public Household? LastHousehold { get; private set; }

        public Task<WeatherObservationData?> FetchAsync(
            WeatherStationIntegration station,
            Household household,
            CancellationToken ct)
        {
            LastStation = station;
            LastHousehold = household;
            return Task.FromResult(result);
        }
    }
}
