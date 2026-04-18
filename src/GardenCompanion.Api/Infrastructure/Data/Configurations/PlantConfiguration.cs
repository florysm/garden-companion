using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.ToTable("Plants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.ExternalId).HasMaxLength(100);
        builder.Property(p => p.ExternalSource).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.CommonName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ScientificName).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.MinSpacingInches).HasColumnType("decimal(6,2)");
        builder.Property(p => p.SunRequirement).HasMaxLength(100);
        builder.Property(p => p.WaterRequirement).HasMaxLength(100);
        builder.Property(p => p.MinDepthInches).HasColumnType("decimal(6,2)");
        builder.Property(p => p.Family).HasMaxLength(100);

        builder.HasIndex(p => p.ExternalSource).HasDatabaseName("IX_Plants_ExternalSource");
        builder.HasIndex(p => p.CommonName).HasDatabaseName("IX_Plants_CommonName");
        builder.HasIndex(p => p.Family).HasDatabaseName("IX_Plants_Family");
        builder.HasIndex(p => new { p.IsGlobal, p.IsApproved }).HasDatabaseName("IX_Plants_IsGlobal");

        builder.HasOne(p => p.ContributedBy)
            .WithMany()
            .HasForeignKey(p => p.ContributedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
