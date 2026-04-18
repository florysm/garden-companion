using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class GardenBed
{
    public Guid Id { get; set; }
    public Guid GardenId { get; set; }
    public string Name { get; set; } = string.Empty;
    public GardenBedType Type { get; set; }
    public GardenBedShape Shape { get; set; }
    public decimal? LengthFeet { get; set; }
    public decimal? WidthFeet { get; set; }
    public decimal? DiameterFeet { get; set; }
    public decimal? DepthInches { get; set; }
    public decimal? VolumeGallons { get; set; }
    public string? SoilType { get; set; }
    public SunExposure SunExposure { get; set; } = SunExposure.FullSun;
    public string? Notes { get; set; }

    // Navigation
    public Garden Garden { get; set; } = null!;
    public ICollection<SoilTest> SoilTests { get; set; } = [];
    public ICollection<Planting> Plantings { get; set; } = [];
    public ICollection<GardenTask> Tasks { get; set; } = [];
    public ICollection<PestDiseaseLog> PestDiseaseLogs { get; set; } = [];
    public ICollection<AmendmentLog> AmendmentLogs { get; set; } = [];
    public ICollection<UserInsight> Insights { get; set; } = [];
}
