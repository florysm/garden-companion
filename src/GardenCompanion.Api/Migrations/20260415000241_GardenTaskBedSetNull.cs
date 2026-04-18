using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GardenCompanion.Api.Migrations
{
    /// <inheritdoc />
    public partial class GardenTaskBedSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GardenTasks_GardenBeds_GardenBedId",
                table: "GardenTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_GardenTasks_GardenBeds_GardenBedId",
                table: "GardenTasks",
                column: "GardenBedId",
                principalTable: "GardenBeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GardenTasks_GardenBeds_GardenBedId",
                table: "GardenTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_GardenTasks_GardenBeds_GardenBedId",
                table: "GardenTasks",
                column: "GardenBedId",
                principalTable: "GardenBeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
