using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GardenCompanion.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantingSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Plantings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Plantings");
        }
    }
}
