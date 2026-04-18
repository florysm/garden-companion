using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class GardenMemberConfiguration : IEntityTypeConfiguration<GardenMember>
{
    public void Configure(EntityTypeBuilder<GardenMember> builder)
    {
        builder.ToTable("GardenMembers");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(m => new { m.GardenId, m.UserId })
            .IsUnique().HasDatabaseName("UQ_GardenMembers_GardenUser");
        builder.HasIndex(m => m.UserId).HasDatabaseName("IX_GardenMembers_UserId");

        builder.HasOne(m => m.Garden)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.GardenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany(u => u.GardenMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.InvitedBy)
            .WithMany()
            .HasForeignKey(m => m.InvitedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
