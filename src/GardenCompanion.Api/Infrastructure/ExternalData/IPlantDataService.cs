namespace GardenCompanion.Api.Infrastructure.ExternalData;

public interface IPlantDataService
{
    Task<List<ExternalPlantResult>> SearchAsync(string query, CancellationToken ct);

    /// <summary>
    /// Fetches a single plant by its ExternalId (format: "{source}:{slug}").
    /// Returns null if the source is unrecognised or the scrape fails.
    /// </summary>
    Task<ExternalPlantResult?> GetAsync(string externalId, CancellationToken ct);
}

public record ExternalPlantResult(
    string ExternalId,
    string CommonName,
    string? ScientificName,
    string? Description,
    decimal? MinSpacingInches,
    string? SunRequirement,
    int? DaysToMaturity,
    int? HeatLevelShu,
    string? WaterRequirement,
    decimal? MinDepthInches,
    string? Family,
    string? FruitSizeDescription = null,
    string? DiseaseResistanceNotes = null,
    string? Aliases = null);
