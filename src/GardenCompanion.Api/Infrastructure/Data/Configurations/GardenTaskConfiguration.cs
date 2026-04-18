using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class GardenTaskConfiguration : IEntityTypeConfiguration<GardenTask>
{
    public void Configure(EntityTypeBuilder<GardenTask> builder)
    {
        builder.ToTable("GardenTasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.TaskType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.GardenId).HasDatabaseName("IX_GardenTasks_GardenId");
        builder.HasIndex(t => t.DueDate).HasDatabaseName("IX_GardenTasks_DueDate");
        builder.HasIndex(t => t.CompletedAt).HasDatabaseName("IX_GardenTasks_CompletedAt");

        builder.HasOne(t => t.Garden)
            .WithMany(g => g.Tasks)
            .HasForeignKey(t => t.GardenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.GardenBed)
            .WithMany(b => b.Tasks)
            .HasForeignKey(t => t.GardenBedId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull); // bed-specific tasks become garden-wide when bed is deleted

        builder.HasOne(t => t.Planting)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.PlantingId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.AssignedTo)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
