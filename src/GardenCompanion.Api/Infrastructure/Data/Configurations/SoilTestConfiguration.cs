using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class SoilTestConfiguration : IEntityTypeConfiguration<SoilTest>
{
    public void Configure(EntityTypeBuilder<SoilTest> builder)
    {
        builder.ToTable("SoilTests");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.TestedAt).IsRequired();
        builder.Property(s => s.PhLevel).HasColumnType("decimal(4,2)");
        builder.Property(s => s.NitrogenPpm).HasColumnType("decimal(8,2)");
        builder.Property(s => s.PhosphorusPpm).HasColumnType("decimal(8,2)");
        builder.Property(s => s.PotassiumPpm).HasColumnType("decimal(8,2)");
        builder.Property(s => s.OrganicMatterPercent).HasColumnType("decimal(5,2)");
        builder.Property(s => s.TestSource).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => s.GardenBedId).HasDatabaseName("IX_SoilTests_GardenBedId");
        builder.HasIndex(s => s.TestedAt).HasDatabaseName("IX_SoilTests_TestedAt");

        builder.HasOne(s => s.GardenBed)
            .WithMany(b => b.SoilTests)
            .HasForeignKey(s => s.GardenBedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
