using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class Plant
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; }
    public ExternalSource ExternalSource { get; set; }
    public Guid? ContributedByUserId { get; set; }
    public bool IsGlobal { get; set; }
    public bool IsApproved { get; set; }
    public string CommonName { get; set; } = string.Empty;
    public string? ScientificName { get; set; }
    public string? Description { get; set; }
    public int? DaysToMaturity { get; set; }
    public decimal? MinSpacingInches { get; set; }
    public string? SunRequirement { get; set; }
    public string? WaterRequirement { get; set; }
    public decimal? MinDepthInches { get; set; }
    public string? Family { get; set; }
    public DateTime? CachedAt { get; set; }

    // Navigation
    public User? ContributedBy { get; set; }
    public ICollection<PlantCompanion> CompanionRelationships { get; set; } = [];
    public ICollection<Planting> Plantings { get; set; } = [];
}
