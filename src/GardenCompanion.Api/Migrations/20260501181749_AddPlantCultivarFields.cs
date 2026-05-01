using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GardenCompanion.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantCultivarFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aliases",
                table: "Plants",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiseaseResistanceNotes",
                table: "Plants",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FruitSizeDescription",
                table: "Plants",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "DiseaseResistanceNotes",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "FruitSizeDescription",
                table: "Plants");
        }
    }
}
