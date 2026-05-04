using System.Text.Json;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather.Providers;

// No credentials required. Reads Latitude/Longitude from Household.
public class OpenMeteoProvider(IHttpClientFactory httpClientFactory, ILogger<OpenMeteoProvider> logger)
    : IWeatherProvider
{
    public WeatherProvider ProviderType => WeatherProvider.OpenMeteo;

    public async Task<WeatherObservationData?> FetchAsync(
        WeatherStationIntegration station,
        Household household,
        CancellationToken ct)
    {
        if (household.Latitude is null || household.Longitude is null)
        {
            logger.LogDebug("Open-Meteo skipped for household {HouseholdId}: no location configured.", household.Id);
            return null;
        }

        var lat = household.Latitude.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        var lon = household.Longitude.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

        var client = httpClientFactory.CreateClient("OpenMeteo");
        var url = $"/v1/forecast?latitude={lat}&longitude={lon}" +
                  "&current=temperature_2m,relative_humidity_2m,wind_speed_10m,wind_direction_10m," +
                  "precipitation,uv_index,dew_point_2m,pressure_msl" +
                  "&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch";

        try
        {
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Open-Meteo returned {Status} for household {HouseholdId}.", response.StatusCode, household.Id);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var current = doc.RootElement.GetProperty("current");

            return new WeatherObservationData(
                ObservedAt: DateTime.UtcNow,
                TemperatureF: current.GetProperty("temperature_2m").GetDecimal(),
                Humidity: current.GetProperty("relative_humidity_2m").GetDecimal(),
                WindSpeedMph: current.GetProperty("wind_speed_10m").GetDecimal(),
                WindDirectionDegrees: current.TryGetProperty("wind_direction_10m", out var wd) ? wd.GetInt32() : null,
                PrecipitationRateInPerHr: current.GetProperty("precipitation").GetDecimal(),
                PrecipitationTotalIn: current.GetProperty("precipitation").GetDecimal(),
                UvIndex: current.TryGetProperty("uv_index", out var uv) ? uv.GetDecimal() : null,
                DewPointF: current.TryGetProperty("dew_point_2m", out var dp) ? dp.GetDecimal() : null,
                PressureInHg: current.TryGetProperty("pressure_msl", out var pr)
                    ? Math.Round(pr.GetDecimal() * 0.02953m, 3)
                    : null,
                StationId: null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Open-Meteo fetch failed for household {HouseholdId}.", household.Id);
            return null;
        }
    }
}
