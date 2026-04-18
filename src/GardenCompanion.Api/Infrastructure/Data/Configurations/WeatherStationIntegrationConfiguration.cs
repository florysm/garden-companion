using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class WeatherStationIntegrationConfiguration : IEntityTypeConfiguration<WeatherStationIntegration>
{
    public void Configure(EntityTypeBuilder<WeatherStationIntegration> builder)
    {
        builder.ToTable("WeatherStationIntegrations");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedOnAdd();
        builder.Property(w => w.Provider).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(w => w.StationId).HasMaxLength(100);
        builder.Property(w => w.ApiKey).HasMaxLength(512);
        builder.Property(w => w.CreatedAt).IsRequired();

        builder.HasIndex(w => w.HouseholdId)
            .IsUnique().HasDatabaseName("UQ_WeatherStationIntegrations_HouseholdId");

        builder.HasOne(w => w.Household)
            .WithMany()
            .HasForeignKey(w => w.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
