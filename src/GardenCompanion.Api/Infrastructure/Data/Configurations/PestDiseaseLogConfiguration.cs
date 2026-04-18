using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class PestDiseaseLogConfiguration : IEntityTypeConfiguration<PestDiseaseLog>
{
    public void Configure(EntityTypeBuilder<PestDiseaseLog> builder)
    {
        builder.ToTable("PestDiseaseLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();
        builder.Property(l => l.ObservedAt).IsRequired();
        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Severity).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(l => l.TreatmentApplied).HasMaxLength(500);
        builder.Property(l => l.Notes).HasMaxLength(1000);

        builder.HasIndex(l => l.GardenBedId).HasDatabaseName("IX_PestDiseaseLogs_GardenBedId");
        builder.HasIndex(l => l.PlantingId).HasDatabaseName("IX_PestDiseaseLogs_PlantingId");
        builder.HasIndex(l => l.ResolvedAt).HasDatabaseName("IX_PestDiseaseLogs_ResolvedAt");

        builder.HasOne(l => l.GardenBed)
            .WithMany(b => b.PestDiseaseLogs)
            .HasForeignKey(l => l.GardenBedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Planting)
            .WithMany(p => p.PestDiseaseLogs)
            .HasForeignKey(l => l.PlantingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
