using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class WeatherObservationConfiguration : IEntityTypeConfiguration<WeatherObservation>
{
    public void Configure(EntityTypeBuilder<WeatherObservation> builder)
    {
        builder.ToTable("WeatherObservations");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedOnAdd();
        builder.Property(w => w.ObservedAt).IsRequired();
        builder.Property(w => w.TemperatureF).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(w => w.Humidity).HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(w => w.WindSpeedMph).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(w => w.PrecipitationRateInPerHr).HasColumnType("decimal(6,3)").IsRequired();
        builder.Property(w => w.PrecipitationTotalIn).HasColumnType("decimal(6,3)").IsRequired();
        builder.Property(w => w.UvIndex).HasColumnType("decimal(4,1)");
        builder.Property(w => w.DewPointF).HasColumnType("decimal(6,2)");
        builder.Property(w => w.PressureInHg).HasColumnType("decimal(6,3)");
        builder.Property(w => w.Source).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(w => w.StationId).HasMaxLength(100);

        builder.HasIndex(w => new { w.HouseholdId, w.ObservedAt })
            .HasDatabaseName("IX_WeatherObservations_HouseholdId_ObservedAt");
        builder.HasIndex(w => w.ObservedAt)
            .HasDatabaseName("IX_WeatherObservations_ObservedAt");

        builder.HasOne(w => w.Household)
            .WithMany(h => h.WeatherObservations)
            .HasForeignKey(w => w.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
