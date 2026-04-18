using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class PlantingObservation
{
    public Guid Id { get; set; }
    public Guid PlantingId { get; set; }
    public ObservationType ObservationType { get; set; } = ObservationType.General;
    public string Note { get; set; } = string.Empty;
    public DateTime ObservedAt { get; set; }

    // Navigation
    public Planting Planting { get; set; } = null!;
}
