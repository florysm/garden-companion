using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class SoilTest
{
    public Guid Id { get; set; }
    public Guid GardenBedId { get; set; }
    public DateOnly TestedAt { get; set; }
    public decimal? PhLevel { get; set; }
    public decimal? NitrogenPpm { get; set; }
    public decimal? PhosphorusPpm { get; set; }
    public decimal? PotassiumPpm { get; set; }
    public decimal? OrganicMatterPercent { get; set; }
    public SoilTestSource TestSource { get; set; } = SoilTestSource.Manual;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public GardenBed GardenBed { get; set; } = null!;
}
