using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class PlantCompanion
{
    public Guid PlantId { get; set; }
    public Guid CompanionPlantId { get; set; }
    public CompanionRelationshipType RelationshipType { get; set; }

    // Navigation
    public Plant Plant { get; set; } = null!;
    public Plant CompanionPlant { get; set; } = null!;
}
