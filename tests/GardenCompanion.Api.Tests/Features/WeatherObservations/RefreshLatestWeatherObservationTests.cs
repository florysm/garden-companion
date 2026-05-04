using GardenCompanion.Api.Features.WeatherObservations;
using GardenCompanion.Api.Infrastructure.ExternalData.Weather;
using Microsoft.Extensions.Logging.Abstractions;

namespace GardenCompanion.Api.Tests.Features.WeatherObservations;

public class RefreshLatestWeatherObservationTests
{
    [Fact]
    public async Task Handle_ReturnsNullWhenHouseholdHasNoWeatherStation()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var user = TestDataFactory.CreateUser();
        var household = TestDataFactory.CreateHousehold(user);
        var member = TestDataFactory.CreateHouseholdMember(household, user, HouseholdRole.Owner);
        db.AddRange(user, household, member);
        await db.SaveChangesAsync();

        var handler = new RefreshLatestWeatherObservationHandler(
            db,
            [],
            NullLogger<RefreshLatestWeatherObservationHandler>.Instance);

        var result = await handler.Handle(
            new RefreshLatestWeatherObservationCommand(household.Id, user.Id),
            CancellationToken.None);

        result.Should().BeNull();
        (await db.WeatherObservations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_FetchesProviderDataAndPersistsObservation()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var user = TestDataFactory.CreateUser();
        var household = TestDataFactory.CreateHousehold(user);
        var member = TestDataFactory.CreateHouseholdMember(household, user, HouseholdRole.Owner);
        db.AddRange(user, household, member);
        await db.SaveChangesAsync();

        var station = new WeatherStationIntegration
        {
            Id = Guid.NewGuid(),
            HouseholdId = household.Id,
            Provider = WeatherProvider.WeatherUnderground,
            StationId = "KPANORTH235",
            ApiKey = "saved-key",
            CreatedAt = DateTime.UtcNow,
        };
        db.WeatherStationIntegrations.Add(station);
        await db.SaveChangesAsync();

        household.WeatherStationIntegrationId = station.Id;
        await db.SaveChangesAsync();

        var observedAt = DateTime.UtcNow.AddMinutes(-3);
        var provider = new FakeWeatherProvider(
            WeatherProvider.WeatherUnderground,
            new WeatherObservationData(
                observedAt,
                73m,
                55m,
                4m,
                203,
                0m,
                0m,
                null,
                56m,
                29.61m,
                "KPANORTH235"));
        var handler = new RefreshLatestWeatherObservationHandler(
            db,
            [provider],
            NullLogger<RefreshLatestWeatherObservationHandler>.Instance);

        var result = await handler.Handle(
            new RefreshLatestWeatherObservationCommand(household.Id, user.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TemperatureF.Should().Be(73m);
        result.Humidity.Should().Be(55m);
        result.StationId.Should().Be("KPANORTH235");

        var stored = await db.WeatherObservations.SingleAsync();
        stored.HouseholdId.Should().Be(household.Id);
        stored.ObservedAt.Should().Be(observedAt);
        stored.Source.Should().Be(WeatherProvider.WeatherUnderground);
        stored.TemperatureF.Should().Be(73m);
    }

    private sealed class FakeWeatherProvider(
        WeatherProvider providerType,
        WeatherObservationData? result) : IWeatherProvider
    {
        public WeatherProvider ProviderType { get; } = providerType;

        public Task<WeatherObservationData?> FetchAsync(
            WeatherStationIntegration station,
            Household household,
            CancellationToken ct) =>
            Task.FromResult(result);
    }
}
