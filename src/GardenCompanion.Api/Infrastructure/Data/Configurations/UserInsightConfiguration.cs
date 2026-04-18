using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class UserInsightConfiguration : IEntityTypeConfiguration<UserInsight>
{
    public void Configure(EntityTypeBuilder<UserInsight> builder)
    {
        builder.ToTable("UserInsights");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        builder.Property(i => i.InsightType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Body).HasMaxLength(1000).IsRequired();
        builder.Property(i => i.GeneratedAt).IsRequired();

        builder.HasIndex(i => new { i.HouseholdId, i.IsRead, i.GeneratedAt })
            .HasDatabaseName("IX_UserInsights_HouseholdId");
        builder.HasIndex(i => i.ExpiresAt)
            .HasDatabaseName("IX_UserInsights_ExpiresAt");

        builder.HasOne(i => i.Household)
            .WithMany(h => h.Insights)
            .HasForeignKey(i => i.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Garden)
            .WithMany(g => g.Insights)
            .HasForeignKey(i => i.GardenId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.GardenBed)
            .WithMany(b => b.Insights)
            .HasForeignKey(i => i.GardenBedId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
