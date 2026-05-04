using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather;

public interface IWeatherProvider
{
    WeatherProvider ProviderType { get; }
    Task<WeatherObservationData?> FetchAsync(
        WeatherStationIntegration station,
        Household household,
        CancellationToken ct);
}
