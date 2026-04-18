using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class HarvestLog
{
    public Guid Id { get; set; }
    public Guid PlantingId { get; set; }
    public Guid HarvestedByUserId { get; set; }
    public DateOnly HarvestDate { get; set; }
    public decimal Quantity { get; set; }
    public QuantityUnit QuantityUnit { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Planting Planting { get; set; } = null!;
    public User HarvestedBy { get; set; } = null!;
}
