using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GardenCompanion.Api.Infrastructure.ExternalData.Weather;

public sealed class WeatherPollingService(
    IServiceScopeFactory scopeFactory,
    IOptions<WeatherPollingOptions> options,
    ILogger<WeatherPollingService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(options.Value.PollIntervalMinutes);
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PollAllHouseholdsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Weather polling cycle failed.");
            }
        }
    }

    private async Task PollAllHouseholdsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<IWeatherProvider>>();

        var stations = await db.WeatherStationIntegrations
            .Include(w => w.Household)
            .ToListAsync(ct);

        logger.LogDebug("Weather polling: {Count} station(s) to poll.", stations.Count);

        foreach (var station in stations)
            await PollOneAsync(db, providers, station, ct);
    }

    private async Task PollOneAsync(
        AppDbContext db,
        IEnumerable<IWeatherProvider> providers,
        WeatherStationIntegration station,
        CancellationToken ct)
    {
        try
        {
            var provider = providers.FirstOrDefault(p => p.ProviderType == station.Provider);
            if (provider is null)
            {
                logger.LogWarning("No provider registered for {Provider}.", station.Provider);
                return;
            }

            var data = await provider.FetchAsync(station, station.Household, ct);
            if (data is null) return;

            db.WeatherObservations.Add(new WeatherObservation
            {
                Id = Guid.NewGuid(),
                HouseholdId = station.HouseholdId,
                ObservedAt = data.ObservedAt,
                TemperatureF = data.TemperatureF,
                Humidity = data.Humidity,
                WindSpeedMph = data.WindSpeedMph,
                WindDirectionDegrees = data.WindDirectionDegrees,
                PrecipitationRateInPerHr = data.PrecipitationRateInPerHr,
                PrecipitationTotalIn = data.PrecipitationTotalIn,
                UvIndex = data.UvIndex,
                DewPointF = data.DewPointF,
                PressureInHg = data.PressureInHg,
                Source = station.Provider,
                StationId = data.StationId,
            });

            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            // Duplicate observation timestamp for this household — discard silently.
            db.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to poll weather for household {HouseholdId}.", station.HouseholdId);
            db.ChangeTracker.Clear();
        }
    }
}
