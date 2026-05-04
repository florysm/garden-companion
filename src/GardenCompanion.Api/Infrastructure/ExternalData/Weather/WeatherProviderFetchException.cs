namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather;

public sealed class WeatherProviderFetchException : InvalidOperationException
{
    public WeatherProviderFetchException(string message) : base(message) { }

    public WeatherProviderFetchException(string message, Exception innerException)
        : base(message, innerException) { }
}
