using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class PlantCompanionConfiguration : IEntityTypeConfiguration<PlantCompanion>
{
    public void Configure(EntityTypeBuilder<PlantCompanion> builder)
    {
        builder.ToTable("PlantCompanions");
        builder.HasKey(pc => new { pc.PlantId, pc.CompanionPlantId });
        builder.Property(pc => pc.RelationshipType)
            .HasConversion<string>().HasMaxLength(20).IsRequired();

        // Explicit self-referencing configuration to avoid cascade delete conflicts.
        builder.HasOne(pc => pc.Plant)
            .WithMany(p => p.CompanionRelationships)
            .HasForeignKey(pc => pc.PlantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.CompanionPlant)
            .WithMany()
            .HasForeignKey(pc => pc.CompanionPlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
