using System.Text.Json;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather.Providers;

// ApiKey field stores "apiKey:apiSecret" (colon-delimited). StationId stores the station ID.
public class DavisWeatherLinkProvider(IHttpClientFactory httpClientFactory, ILogger<DavisWeatherLinkProvider> logger)
    : IWeatherProvider
{
    public WeatherProvider ProviderType => WeatherProvider.DavisWeatherLink;

    public async Task<WeatherObservationData?> FetchAsync(
        WeatherStationIntegration station,
        Household household,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(station.StationId) || string.IsNullOrWhiteSpace(station.ApiKey))
        {
            logger.LogWarning("DavisWeatherLink station {StationId}: missing StationId or ApiKey.", station.StationId);
            return null;
        }

        var parts = station.ApiKey.Split(':', 2);
        if (parts.Length != 2)
        {
            logger.LogWarning("DavisWeatherLink station {StationId}: ApiKey must be 'apiKey:apiSecret'.", station.StationId);
            return null;
        }

        var apiKey = parts[0];
        var apiSecret = parts[1];
        var client = httpClientFactory.CreateClient("DavisWeatherLink");
        var url = $"https://api.weatherlink.com/v2/current/{Uri.EscapeDataString(station.StationId)}?api-key={Uri.EscapeDataString(apiKey)}&api-secret={Uri.EscapeDataString(apiSecret)}";

        try
        {
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("DavisWeatherLink returned {Status} for station {StationId}.", response.StatusCode, station.StationId);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            // Find the first sensor data record that has a temp field
            JsonElement? data = null;
            foreach (var sensor in doc.RootElement.GetProperty("sensors").EnumerateArray())
            {
                if (!sensor.TryGetProperty("data", out var dataArr) || dataArr.GetArrayLength() == 0) continue;
                var d = dataArr[0];
                if (d.TryGetProperty("temp", out _)) { data = d; break; }
            }

            if (data is null)
            {
                logger.LogWarning("DavisWeatherLink: no usable sensor data for station {StationId}.", station.StationId);
                return null;
            }

            var d2 = data.Value;

            return new WeatherObservationData(
                ObservedAt: DateTime.UtcNow,
                TemperatureF: d2.GetProperty("temp").GetDecimal(),
                Humidity: d2.GetProperty("hum").GetDecimal(),
                WindSpeedMph: d2.TryGetProperty("wind_speed_avg_last_2_min", out var ws) ? ws.GetDecimal() : 0m,
                WindDirectionDegrees: d2.TryGetProperty("wind_dir_scalar_avg_last_2_min", out var wd) ? wd.GetInt32() : null,
                PrecipitationRateInPerHr: d2.TryGetProperty("rainfall_last_60_min", out var pr) ? pr.GetDecimal() : 0m,
                PrecipitationTotalIn: d2.TryGetProperty("rainfall_daily_in", out var pt) ? pt.GetDecimal() : 0m,
                UvIndex: d2.TryGetProperty("uv_index", out var uv) ? uv.GetDecimal() : null,
                DewPointF: d2.TryGetProperty("dew_point", out var dp) ? dp.GetDecimal() : null,
                PressureInHg: d2.TryGetProperty("bar_sea_level", out var bp) ? bp.GetDecimal() : null,
                StationId: station.StationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DavisWeatherLink fetch failed for station {StationId}.", station.StationId);
            return null;
        }
    }
}
