using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class PlantingConfiguration : IEntityTypeConfiguration<Planting>
{
    public void Configure(EntityTypeBuilder<Planting> builder)
    {
        builder.ToTable("Plantings");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.PlantedDate).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.PlantingType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.SeasonType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(p => p.GardenBedId).HasDatabaseName("IX_Plantings_GardenBedId");
        builder.HasIndex(p => p.PlantId).HasDatabaseName("IX_Plantings_PlantId");
        builder.HasIndex(p => p.Status).HasDatabaseName("IX_Plantings_Status");
        builder.HasIndex(p => new { p.SeasonYear, p.SeasonType }).HasDatabaseName("IX_Plantings_Season");

        // Soft delete global query filter
        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasOne(p => p.GardenBed)
            .WithMany(b => b.Plantings)
            .HasForeignKey(p => p.GardenBedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Plant)
            .WithMany(pl => pl.Plantings)
            .HasForeignKey(p => p.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
