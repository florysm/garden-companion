using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class GardenConfiguration : IEntityTypeConfiguration<Garden>
{
    public void Configure(EntityTypeBuilder<Garden> builder)
    {
        builder.ToTable("Gardens");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedOnAdd();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(500);
        builder.Property(g => g.CreatedAt).IsRequired();

        builder.HasIndex(g => g.HouseholdId).HasDatabaseName("IX_Gardens_HouseholdId");

        builder.HasOne(g => g.Household)
            .WithMany(h => h.Gardens)
            .HasForeignKey(g => g.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.GardenTypes)
            .WithMany(t => t.Gardens)
            .UsingEntity(j => j.ToTable("GardenGardenTypes"));
    }
}
