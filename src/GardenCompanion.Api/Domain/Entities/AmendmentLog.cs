using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class AmendmentLog
{
    public Guid Id { get; set; }
    public Guid GardenBedId { get; set; }
    public Guid? PlantingId { get; set; }
    public DateOnly AppliedAt { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public AmendmentType AmendmentType { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public GardenBed GardenBed { get; set; } = null!;
    public Planting? Planting { get; set; }
}
