using GardenCompanion.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GardenCompanion.Api.Infrastructure.Data.Configurations;

public class GardenTypeConfiguration : IEntityTypeConfiguration<GardenType>
{
    public void Configure(EntityTypeBuilder<GardenType> builder)
    {
        builder.ToTable("GardenTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique().HasDatabaseName("UQ_GardenTypes_Name");

        builder.HasData(
            new GardenType { Id = 1, Name = "Vegetable" },
            new GardenType { Id = 2, Name = "Fruit" },
            new GardenType { Id = 3, Name = "Herb" },
            new GardenType { Id = 4, Name = "Flower" },
            new GardenType { Id = 5, Name = "Orchard" },
            new GardenType { Id = 6, Name = "Greenhouse" },
            new GardenType { Id = 7, Name = "Other" }
        );
    }
}
