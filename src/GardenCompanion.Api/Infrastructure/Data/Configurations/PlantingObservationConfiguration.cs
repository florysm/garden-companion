using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class PlantingObservationConfiguration : IEntityTypeConfiguration<PlantingObservation>
{
    public void Configure(EntityTypeBuilder<PlantingObservation> builder)
    {
        builder.ToTable("PlantingObservations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();
        builder.Property(o => o.ObservationType)
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.Note).HasMaxLength(2000).IsRequired();
        builder.Property(o => o.ObservedAt).IsRequired();

        builder.HasIndex(o => o.PlantingId).HasDatabaseName("IX_PlantingObservations_PlantingId");
        builder.HasIndex(o => o.ObservedAt).HasDatabaseName("IX_PlantingObservations_ObservedAt");

        builder.HasOne(o => o.Planting)
            .WithMany(p => p.Observations)
            .HasForeignKey(o => o.PlantingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
