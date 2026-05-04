using System.Text.Json;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather.Providers;

public class WeatherUndergroundProvider(IHttpClientFactory httpClientFactory, ILogger<WeatherUndergroundProvider> logger)
    : IWeatherProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public WeatherProvider ProviderType => WeatherProvider.WeatherUnderground;

    public async Task<WeatherObservationData?> FetchAsync(
        WeatherStationIntegration station,
        Household household,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(station.StationId) || string.IsNullOrWhiteSpace(station.ApiKey))
        {
            logger.LogWarning("WeatherUnderground station {StationId}: missing StationId or ApiKey.", station.StationId);
            return null;
        }

        var client = httpClientFactory.CreateClient("WeatherUnderground");
        var url = $"https://api.weather.com/v2/pws/observations/current?stationId={Uri.EscapeDataString(station.StationId)}&format=json&units=e&apiKey={Uri.EscapeDataString(station.ApiKey)}";

        try
        {
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("WeatherUnderground returned {Status} for station {StationId}.", response.StatusCode, station.StationId);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var obs = doc.RootElement.GetProperty("observations")[0];
            var imperial = obs.GetProperty("imperial");

            return new WeatherObservationData(
                ObservedAt: DateTime.UtcNow,
                TemperatureF: imperial.GetProperty("temp").GetDecimal(),
                Humidity: obs.GetProperty("humidity").GetDecimal(),
                WindSpeedMph: imperial.GetProperty("windSpeed").GetDecimal(),
                WindDirectionDegrees: obs.TryGetProperty("winddir", out var wd) ? wd.GetInt32() : null,
                PrecipitationRateInPerHr: imperial.GetProperty("precipRate").GetDecimal(),
                PrecipitationTotalIn: imperial.GetProperty("precipTotal").GetDecimal(),
                UvIndex: obs.TryGetProperty("uv", out var uv) ? uv.GetDecimal() : null,
                DewPointF: imperial.TryGetProperty("dewpt", out var dp) ? dp.GetDecimal() : null,
                PressureInHg: imperial.TryGetProperty("pressure", out var pr) ? pr.GetDecimal() : null,
                StationId: station.StationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WeatherUnderground fetch failed for station {StationId}.", station.StationId);
            return null;
        }
    }
}
