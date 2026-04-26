using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class Planting
{
    public Guid Id { get; set; }
    public Guid GardenBedId { get; set; }
    public Guid PlantId { get; set; }
    public DateOnly PlantedDate { get; set; }
    public DateOnly? ExpectedHarvestDate { get; set; }
    public DateOnly? ActualEndDate { get; set; }
    public PlantingStatus Status { get; set; } = PlantingStatus.Planted;
    public PlantingType PlantingType { get; set; } = PlantingType.Annual;
    public PlantingSource Source { get; set; } = PlantingSource.DirectSeed;
    public int Quantity { get; set; } = 1;
    public int SeasonYear { get; set; }
    public SeasonType SeasonType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public GardenBed GardenBed { get; set; } = null!;
    public Plant Plant { get; set; } = null!;
    public ICollection<PlantingObservation> Observations { get; set; } = [];
    public ICollection<HarvestLog> HarvestLogs { get; set; } = [];
    public ICollection<PestDiseaseLog> PestDiseaseLogs { get; set; } = [];
    public ICollection<AmendmentLog> AmendmentLogs { get; set; } = [];
    public ICollection<GardenTask> Tasks { get; set; } = [];
}
