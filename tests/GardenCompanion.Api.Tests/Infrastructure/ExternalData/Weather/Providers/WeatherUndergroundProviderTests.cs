using GardenCompanion.Api.Infrastructure.ExternalData.Weather;
using GardenCompanion.Api.Infrastructure.ExternalData.Weather.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace GardenCompanion.Api.Tests.Infrastructure.ExternalData.Weather.Providers;

public class WeatherUndergroundProviderTests
{
    [Fact]
    public async Task FetchAsync_ThrowsSafeDetailsWhenUpstreamReturnsFailure()
    {
        const string apiKey = "secret-api-key";
        var provider = new WeatherUndergroundProvider(
            new FakeHttpClientFactory(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                ReasonPhrase = "Unauthorized",
                Content = new StringContent("""{"error":"invalid apiKey secret-api-key"}"""),
            }),
            NullLogger<WeatherUndergroundProvider>.Instance);

        var station = new WeatherStationIntegration
        {
            Id = Guid.NewGuid(),
            HouseholdId = Guid.NewGuid(),
            Provider = WeatherProvider.WeatherUnderground,
            StationId = "KTEST123",
            ApiKey = apiKey,
            CreatedAt = DateTime.UtcNow,
        };
        var household = new Household
        {
            Id = station.HouseholdId,
            Name = "Test Household",
            OwnedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        var act = () => provider.FetchAsync(station, household, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<WeatherProviderFetchException>();
        ex.Which.Message.Should().Contain("Weather Underground returned 401");
        ex.Which.Message.Should().Contain("[redacted]");
        ex.Which.Message.Should().NotContain(apiKey);
    }

    [Fact]
    public async Task FetchAsync_ThrowsDetailsWhenSuccessfulResponseHasNoObservations()
    {
        const string apiKey = "secret-api-key";
        var provider = new WeatherUndergroundProvider(
            new FakeHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"observations":[]}"""),
            }),
            NullLogger<WeatherUndergroundProvider>.Instance);

        var station = new WeatherStationIntegration
        {
            Id = Guid.NewGuid(),
            HouseholdId = Guid.NewGuid(),
            Provider = WeatherProvider.WeatherUnderground,
            StationId = "KTEST123",
            ApiKey = apiKey,
            CreatedAt = DateTime.UtcNow,
        };
        var household = new Household
        {
            Id = station.HouseholdId,
            Name = "Test Household",
            OwnedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        var act = () => provider.FetchAsync(station, household, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<WeatherProviderFetchException>();
        ex.Which.Message.Should().Contain("returned 200 but no observations were present");
        ex.Which.Message.Should().Contain("""{"observations":[]}""");
        ex.Which.Message.Should().NotContain(apiKey);
    }

    [Fact]
    public async Task FetchAsync_ParsesSuccessfulResponseWithNullOptionalValues()
    {
        var provider = new WeatherUndergroundProvider(
            new FakeHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"observations":[{"stationID":"KPANORTH235","humidity":55,"uv":null,"winddir":203,"imperial":{"temp":73,"dewpt":56,"windSpeed":4,"pressure":29.61,"precipRate":0.00,"precipTotal":0.00}}]}
                    """),
            }),
            NullLogger<WeatherUndergroundProvider>.Instance);

        var station = new WeatherStationIntegration
        {
            Id = Guid.NewGuid(),
            HouseholdId = Guid.NewGuid(),
            Provider = WeatherProvider.WeatherUnderground,
            StationId = "KPANORTH235",
            ApiKey = "secret-api-key",
            CreatedAt = DateTime.UtcNow,
        };
        var household = new Household
        {
            Id = station.HouseholdId,
            Name = "Test Household",
            OwnedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

        var result = await provider.FetchAsync(station, household, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TemperatureF.Should().Be(73);
        result.Humidity.Should().Be(55);
        result.WindSpeedMph.Should().Be(4);
        result.WindDirectionDegrees.Should().Be(203);
        result.UvIndex.Should().BeNull();
        result.DewPointF.Should().Be(56);
        result.PressureInHg.Should().Be(29.61m);
        result.StationId.Should().Be("KPANORTH235");
    }

    private sealed class FakeHttpClientFactory(HttpResponseMessage response) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StaticResponseHandler(response));
    }

    private sealed class StaticResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
