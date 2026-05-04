using System.Text.Json;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather.Providers;

// ApiKey field stores "applicationKey:userApiKey" (colon-delimited).
// StationId stores the device MAC address.
public class AmbientWeatherProvider(IHttpClientFactory httpClientFactory, ILogger<AmbientWeatherProvider> logger)
    : IWeatherProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public WeatherProvider ProviderType => WeatherProvider.AmbientWeather;

    public async Task<WeatherObservationData?> FetchAsync(
        WeatherStationIntegration station,
        Household household,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(station.StationId) || string.IsNullOrWhiteSpace(station.ApiKey))
        {
            logger.LogWarning("AmbientWeather station {StationId}: missing StationId or ApiKey.", station.StationId);
            return null;
        }

        var parts = station.ApiKey.Split(':', 2);
        if (parts.Length != 2)
        {
            logger.LogWarning("AmbientWeather station {StationId}: ApiKey must be 'applicationKey:userApiKey'.", station.StationId);
            return null;
        }

        var appKey = parts[0];
        var userApiKey = parts[1];
        var mac = Uri.EscapeDataString(station.StationId);
        var client = httpClientFactory.CreateClient("AmbientWeather");
        var url = $"https://rt.ambientweather.net/v1/devices/{mac}?apiKey={Uri.EscapeDataString(userApiKey)}&applicationKey={Uri.EscapeDataString(appKey)}&limit=1";

        try
        {
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("AmbientWeather returned {Status} for station {StationId}.", response.StatusCode, station.StationId);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;
            if (root.GetArrayLength() == 0) return null;

            var last = root[0].GetProperty("lastData");

            return new WeatherObservationData(
                ObservedAt: DateTime.UtcNow,
                TemperatureF: last.GetProperty("tempf").GetDecimal(),
                Humidity: last.GetProperty("humidity").GetDecimal(),
                WindSpeedMph: last.GetProperty("windspeedmph").GetDecimal(),
                WindDirectionDegrees: last.TryGetProperty("winddir", out var wd) ? wd.GetInt32() : null,
                PrecipitationRateInPerHr: last.TryGetProperty("hourlyrainin", out var pr) ? pr.GetDecimal() : 0m,
                PrecipitationTotalIn: last.TryGetProperty("dailyrainin", out var pt) ? pt.GetDecimal() : 0m,
                UvIndex: last.TryGetProperty("uv", out var uv) ? uv.GetDecimal() : null,
                DewPointF: last.TryGetProperty("dewPoint", out var dp) ? dp.GetDecimal() : null,
                PressureInHg: last.TryGetProperty("baromrelin", out var bp) ? bp.GetDecimal() : null,
                StationId: station.StationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AmbientWeather fetch failed for station {StationId}.", station.StationId);
            return null;
        }
    }
}
