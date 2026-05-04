using System.Text.Json;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather.Providers;

public class WeatherUndergroundProvider(IHttpClientFactory httpClientFactory, ILogger<WeatherUndergroundProvider> logger)
    : IWeatherProvider
{
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
                var failedResponseBody = await response.Content.ReadAsStringAsync(ct);
                var safeBody = FormatSafeBody(failedResponseBody, station.ApiKey);
                logger.LogWarning(
                    "WeatherUnderground returned {Status} for station {StationId}. Response: {ResponseBody}",
                    response.StatusCode,
                    station.StationId,
                    safeBody);

                throw new WeatherProviderFetchException(
                    $"Weather Underground returned {(int)response.StatusCode} ({response.ReasonPhrase}). {safeBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var safeResponseBody = FormatSafeBody(responseBody, station.ApiKey);
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (!doc.RootElement.TryGetProperty("observations", out var observations) ||
                    observations.ValueKind != JsonValueKind.Array ||
                    observations.GetArrayLength() == 0)
                {
                    logger.LogWarning(
                        "WeatherUnderground returned no observations for station {StationId}. Response: {ResponseBody}",
                        station.StationId,
                        safeResponseBody);
                    throw new WeatherProviderFetchException(
                        $"Weather Underground returned 200 but no observations were present. {safeResponseBody}");
                }

                var obs = observations[0];
                if (!obs.TryGetProperty("imperial", out var imperial))
                {
                    logger.LogWarning(
                        "WeatherUnderground returned no imperial units block for station {StationId}. Response: {ResponseBody}",
                        station.StationId,
                        safeResponseBody);
                    throw new WeatherProviderFetchException(
                        $"Weather Underground returned 200 but no imperial units block was present. {safeResponseBody}");
                }

                return new WeatherObservationData(
                    ObservedAt: DateTime.UtcNow,
                    TemperatureF: imperial.GetProperty("temp").GetDecimal(),
                    Humidity: obs.GetProperty("humidity").GetDecimal(),
                    WindSpeedMph: imperial.GetProperty("windSpeed").GetDecimal(),
                    WindDirectionDegrees: TryGetInt32(obs, "winddir"),
                    PrecipitationRateInPerHr: imperial.GetProperty("precipRate").GetDecimal(),
                    PrecipitationTotalIn: imperial.GetProperty("precipTotal").GetDecimal(),
                    UvIndex: TryGetDecimal(obs, "uv"),
                    DewPointF: TryGetDecimal(imperial, "dewpt"),
                    PressureInHg: TryGetDecimal(imperial, "pressure"),
                    StationId: station.StationId);
            }
            catch (WeatherProviderFetchException) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "WeatherUnderground returned an unparsable response for station {StationId}. Response: {ResponseBody}",
                    station.StationId,
                    safeResponseBody);
                throw new WeatherProviderFetchException(
                    $"Weather Underground returned 200 but the response could not be parsed. {safeResponseBody}",
                    ex);
            }
        }
        catch (WeatherProviderFetchException) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WeatherUnderground fetch failed for station {StationId}.", station.StationId);
            return null;
        }
    }

    private static string FormatSafeBody(string? responseBody, string? apiKey)
    {
        var safeBody = string.IsNullOrWhiteSpace(responseBody)
            ? "No response body returned."
            : responseBody.Trim();

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            safeBody = safeBody.Replace(apiKey, "[redacted]", StringComparison.Ordinal);
            safeBody = safeBody.Replace(Uri.EscapeDataString(apiKey), "[redacted]", StringComparison.Ordinal);
        }

        safeBody = string.Join(' ', safeBody.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return safeBody.Length <= 500 ? safeBody : $"{safeBody[..500]}...";
    }

    private static decimal? TryGetDecimal(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal()
            : null;

    private static int? TryGetInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
}
