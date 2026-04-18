using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.PreferredLanguage).HasMaxLength(10).IsRequired();
        builder.Property(s => s.TemperatureUnit)
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.LengthUnit)
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.WeightUnit)
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.VolumeUnit)
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.UsdaHardinessZone).HasMaxLength(10);
        builder.Property(s => s.LocationLatitude).HasColumnType("decimal(9,6)");
        builder.Property(s => s.LocationLongitude).HasColumnType("decimal(9,6)");

        builder.HasIndex(s => s.UserId).IsUnique().HasDatabaseName("UQ_UserSettings_UserId");
    }
}
