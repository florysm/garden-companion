using GardenCompanion.Api.Infrastructure.ExternalData;

namespace GardenCompanion.Api.Infrastructure.Data;

public sealed class PlantSeedingService(
    IServiceScopeFactory scopeFactory,
    ILogger<PlantSeedingService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var plantDataService = scope.ServiceProvider.GetRequiredService<IPlantDataService>();
            await PlantSeeder.SeedAsync(db, plantDataService, logger, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Plant seeding failed.");
        }
    }
}
