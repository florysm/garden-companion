using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("Households");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedOnAdd();
        builder.Property(h => h.Name).HasMaxLength(100).IsRequired();
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasIndex(h => h.OwnedByUserId).HasDatabaseName("IX_Households_OwnedByUserId");

        builder.HasOne(h => h.Owner)
            .WithMany(u => u.OwnedHouseholds)
            .HasForeignKey(h => h.OwnedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Circular FK: Household.WeatherStationIntegrationId → WeatherStationIntegrations
        // Configured with SetNull; the inverse is configured in WeatherStationIntegrationConfiguration.
        builder.HasOne(h => h.WeatherStationIntegration)
            .WithOne()
            .HasForeignKey<Household>(h => h.WeatherStationIntegrationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
