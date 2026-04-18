using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class HarvestLogConfiguration : IEntityTypeConfiguration<HarvestLog>
{
    public void Configure(EntityTypeBuilder<HarvestLog> builder)
    {
        builder.ToTable("HarvestLogs");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedOnAdd();
        builder.Property(h => h.HarvestDate).IsRequired();
        builder.Property(h => h.Quantity).HasColumnType("decimal(10,3)").IsRequired();
        builder.Property(h => h.QuantityUnit).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(h => h.Notes).HasMaxLength(500);
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasIndex(h => h.PlantingId).HasDatabaseName("IX_HarvestLogs_PlantingId");
        builder.HasIndex(h => h.HarvestDate).HasDatabaseName("IX_HarvestLogs_HarvestDate");
        builder.HasIndex(h => h.HarvestedByUserId).HasDatabaseName("IX_HarvestLogs_HarvestedByUserId");

        builder.HasOne(h => h.Planting)
            .WithMany(p => p.HarvestLogs)
            .HasForeignKey(h => h.PlantingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.HarvestedBy)
            .WithMany(u => u.HarvestLogs)
            .HasForeignKey(h => h.HarvestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
