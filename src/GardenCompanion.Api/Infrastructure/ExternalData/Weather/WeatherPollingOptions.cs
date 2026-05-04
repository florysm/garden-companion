namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather;

public class WeatherPollingOptions
{
    public int PollIntervalMinutes { get; set; } = 15;
}
