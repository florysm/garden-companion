using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class GardenBedConfiguration : IEntityTypeConfiguration<GardenBed>
{
    public void Configure(EntityTypeBuilder<GardenBed> builder)
    {
        builder.ToTable("GardenBeds");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(b => b.Shape).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.LengthFeet).HasColumnType("decimal(6,2)");
        builder.Property(b => b.WidthFeet).HasColumnType("decimal(6,2)");
        builder.Property(b => b.DiameterFeet).HasColumnType("decimal(6,2)");
        builder.Property(b => b.DepthInches).HasColumnType("decimal(6,2)");
        builder.Property(b => b.VolumeGallons).HasColumnType("decimal(8,2)");
        builder.Property(b => b.SoilType).HasMaxLength(100);
        builder.Property(b => b.SunExposure).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.HasIndex(b => b.GardenId).HasDatabaseName("IX_GardenBeds_GardenId");

        builder.HasOne(b => b.Garden)
            .WithMany(g => g.Beds)
            .HasForeignKey(b => b.GardenId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
