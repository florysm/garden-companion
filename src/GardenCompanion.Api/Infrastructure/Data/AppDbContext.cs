using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Identity & Sharing
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<WeatherStationIntegration> WeatherStationIntegrations => Set<WeatherStationIntegration>();

    // Garden & Beds
    public DbSet<Garden> Gardens => Set<Garden>();
    public DbSet<GardenType> GardenTypes => Set<GardenType>();
    public DbSet<GardenMember> GardenMembers => Set<GardenMember>();
    public DbSet<GardenBed> GardenBeds => Set<GardenBed>();
    public DbSet<SoilTest> SoilTests => Set<SoilTest>();
    public DbSet<GardenTask> GardenTasks => Set<GardenTask>();

    // Plants & Plantings
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<PlantCompanion> PlantCompanions => Set<PlantCompanion>();
    public DbSet<Planting> Plantings => Set<Planting>();
    public DbSet<PlantingObservation> PlantingObservations => Set<PlantingObservation>();
    public DbSet<PestDiseaseLog> PestDiseaseLogs => Set<PestDiseaseLog>();
    public DbSet<AmendmentLog> AmendmentLogs => Set<AmendmentLog>();
    public DbSet<HarvestLog> HarvestLogs => Set<HarvestLog>();

    // Weather & Insights
    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();
    public DbSet<UserInsight> UserInsights => Set<UserInsight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
