using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GardenCompanion.Api.Migrations
{
    /// <inheritdoc />
    public partial class HashRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Purge raw tokens — existing rows cannot be resolved against hashed lookups.
            // All active sessions will require a new login after this migration.
            migrationBuilder.Sql("DELETE FROM UserRefreshTokens;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot reverse hashing; purge again to avoid phantom failures on rollback.
            migrationBuilder.Sql("DELETE FROM UserRefreshTokens;");
        }
    }
}
