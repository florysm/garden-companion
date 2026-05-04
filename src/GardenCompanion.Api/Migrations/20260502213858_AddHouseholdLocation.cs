using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GardenCompanion.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Households",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Households",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Households");
        }
    }
}
