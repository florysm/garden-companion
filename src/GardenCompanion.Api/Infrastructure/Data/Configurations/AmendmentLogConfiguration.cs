using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class AmendmentLogConfiguration : IEntityTypeConfiguration<AmendmentLog>
{
    public void Configure(EntityTypeBuilder<AmendmentLog> builder)
    {
        builder.ToTable("AmendmentLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();
        builder.Property(a => a.AppliedAt).IsRequired();
        builder.Property(a => a.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.AmendmentType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.Quantity).HasColumnType("decimal(10,3)").IsRequired();
        builder.Property(a => a.QuantityUnit).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.HasIndex(a => a.GardenBedId).HasDatabaseName("IX_AmendmentLogs_GardenBedId");
        builder.HasIndex(a => a.AppliedAt).HasDatabaseName("IX_AmendmentLogs_AppliedAt");

        builder.HasOne(a => a.GardenBed)
            .WithMany(b => b.AmendmentLogs)
            .HasForeignKey(a => a.GardenBedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Planting)
            .WithMany(p => p.AmendmentLogs)
            .HasForeignKey(a => a.PlantingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
