using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class PestDiseaseLog
{
    public Guid Id { get; set; }
    public Guid? PlantingId { get; set; }
    public Guid GardenBedId { get; set; }
    public DateTime ObservedAt { get; set; }
    public PestDiseaseType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.Low;
    public string? TreatmentApplied { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Planting? Planting { get; set; }
    public GardenBed GardenBed { get; set; } = null!;
}
