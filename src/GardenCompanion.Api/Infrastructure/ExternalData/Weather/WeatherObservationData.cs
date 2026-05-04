namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather;

public record WeatherObservationData(
    DateTime ObservedAt,
    decimal TemperatureF,
    decimal Humidity,
    decimal WindSpeedMph,
    int? WindDirectionDegrees,
    decimal PrecipitationRateInPerHr,
    decimal PrecipitationTotalIn,
    decimal? UvIndex,
    decimal? DewPointF,
    decimal? PressureInHg,
    string? StationId);
