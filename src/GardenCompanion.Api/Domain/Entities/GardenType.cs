namespace GardenCompanion.Api.Domain.Entities;

public class GardenType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Garden> Gardens { get; set; } = [];
}
