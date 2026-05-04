namespace GardenCompanion.Api.Domain.Entities;

public class Household
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OwnedByUserId { get; set; }
    public Guid? WeatherStationIntegrationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Navigation
    public User Owner { get; set; } = null!;
    public WeatherStationIntegration? WeatherStationIntegration { get; set; }
    public ICollection<HouseholdMember> Members { get; set; } = [];
    public ICollection<Garden> Gardens { get; set; } = [];
    public ICollection<WeatherObservation> WeatherObservations { get; set; } = [];
    public ICollection<UserInsight> Insights { get; set; } = [];
}
