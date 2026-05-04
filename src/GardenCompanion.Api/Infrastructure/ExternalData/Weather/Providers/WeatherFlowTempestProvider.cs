using System.Text.Json;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather.Providers;

// ApiKey stores the personal access token. StationId stores the station ID / serial.
public class WeatherFlowTempestProvider(IHttpClientFactory httpClientFactory, ILogger<WeatherFlowTempestProvider> logger)
    : IWeatherProvider
{
    public WeatherProvider ProviderType => WeatherProvider.WeatherFlowTempest;

    public async Task<WeatherObservationData?> FetchAsync(
        WeatherStationIntegration station,
        Household household,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(station.StationId) || string.IsNullOrWhiteSpace(station.ApiKey))
        {
            logger.LogWarning("WeatherFlow station {StationId}: missing StationId or token.", station.StationId);
            return null;
        }

        var client = httpClientFactory.CreateClient("WeatherFlowTempest");
        var url = $"https://swd.weatherflow.com/swd/rest/observations/station/{Uri.EscapeDataString(station.StationId)}?token={Uri.EscapeDataString(station.ApiKey)}";

        try
        {
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("WeatherFlow returned {Status} for station {StationId}.", response.StatusCode, station.StationId);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var obs = doc.RootElement.GetProperty("obs")[0];

            // WeatherFlow Tempest obs array (positional):
            // [0]=epoch, [1]=wind_lull, [2]=wind_avg(m/s), [3]=wind_gust, [4]=wind_dir,
            // [5]=wind_sample_interval, [6]=pressure(mb), [7]=air_temp(C), [8]=humidity,
            // [9]=illuminance, [10]=uv, [11]=solar_radiation,
            // [12]=precip_accumulated(mm), [13]=precip_type, [14]=lightning_avg_dist,
            // [15]=lightning_count, [16]=battery, [17]=report_interval

            decimal CToF(decimal c) => c * 9m / 5m + 32m;
            decimal MsToMph(decimal ms) => ms * 2.23694m;
            decimal MbToInHg(decimal mb) => mb * 0.02953m;
            decimal MmToIn(decimal mm) => mm * 0.0393701m;

            var airTempC = obs[7].GetDecimal();
            var windMs = obs[2].GetDecimal();
            var pressureMb = obs[6].GetDecimal();
            var precipMm = obs[12].GetDecimal();

            // Derive dew point from temp + humidity using Magnus formula
            var humidity = obs[8].GetDecimal();
            decimal? dewPointF = null;
            if (humidity > 0)
            {
                var a = 17.625m;
                var b = 243.04m;
                var gamma = (decimal)Math.Log((double)(humidity / 100m)) + a * airTempC / (b + airTempC);
                var dewC = b * gamma / (a - gamma);
                dewPointF = CToF(dewC);
            }

            return new WeatherObservationData(
                ObservedAt: DateTime.UtcNow,
                TemperatureF: CToF(airTempC),
                Humidity: humidity,
                WindSpeedMph: MsToMph(windMs),
                WindDirectionDegrees: (int)obs[4].GetDecimal(),
                PrecipitationRateInPerHr: MmToIn(precipMm),
                PrecipitationTotalIn: MmToIn(precipMm),
                UvIndex: obs[10].GetDecimal(),
                DewPointF: dewPointF,
                PressureInHg: MbToInHg(pressureMb),
                StationId: station.StationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WeatherFlow fetch failed for station {StationId}.", station.StationId);
            return null;
        }
    }
}
