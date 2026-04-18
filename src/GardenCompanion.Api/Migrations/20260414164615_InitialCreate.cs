using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GardenCompanion.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GardenTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GardenTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ExternalSource = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ContributedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommonName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ScientificName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DaysToMaturity = table.Column<int>(type: "INTEGER", nullable: true),
                    MinSpacingInches = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    SunRequirement = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WaterRequirement = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MinDepthInches = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Family = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plants_Users_ContributedByUserId",
                        column: x => x.ContributedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationLatitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    LocationLongitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TemperatureUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LengthUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WeightUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    VolumeUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    UsdaHardinessZone = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    AverageFrostDateSpring = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AverageFrostDateFall = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ShareWeatherData = table.Column<bool>(type: "INTEGER", nullable: false),
                    SharePlantingData = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShareHarvestData = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantCompanions",
                columns: table => new
                {
                    PlantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanionPlantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationshipType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantCompanions", x => new { x.PlantId, x.CompanionPlantId });
                    table.ForeignKey(
                        name: "FK_PlantCompanions_Plants_CompanionPlantId",
                        column: x => x.CompanionPlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlantCompanions_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AmendmentLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GardenBedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlantingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AppliedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AmendmentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    QuantityUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmendmentLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GardenBeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GardenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Shape = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LengthFeet = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    WidthFeet = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    DiameterFeet = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    DepthInches = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    VolumeGallons = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    SoilType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SunExposure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GardenBeds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plantings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GardenBedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlantedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpectedHarvestDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ActualEndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PlantingType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    SeasonYear = table.Column<int>(type: "INTEGER", nullable: false),
                    SeasonType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plantings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plantings_GardenBeds_GardenBedId",
                        column: x => x.GardenBedId,
                        principalTable: "GardenBeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plantings_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoilTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GardenBedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TestedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PhLevel = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    NitrogenPpm = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    PhosphorusPpm = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    PotassiumPpm = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    OrganicMatterPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TestSource = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoilTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoilTests_GardenBeds_GardenBedId",
                        column: x => x.GardenBedId,
                        principalTable: "GardenBeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HarvestLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlantingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HarvestedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HarvestDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    QuantityUnit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HarvestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HarvestLogs_Plantings_PlantingId",
                        column: x => x.PlantingId,
                        principalTable: "Plantings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HarvestLogs_Users_HarvestedByUserId",
                        column: x => x.HarvestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PestDiseaseLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlantingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GardenBedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TreatmentApplied = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PestDiseaseLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PestDiseaseLogs_GardenBeds_GardenBedId",
                        column: x => x.GardenBedId,
                        principalTable: "GardenBeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PestDiseaseLogs_Plantings_PlantingId",
                        column: x => x.PlantingId,
                        principalTable: "Plantings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlantingObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlantingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObservationType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantingObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantingObservations_Plantings_PlantingId",
                        column: x => x.PlantingId,
                        principalTable: "Plantings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GardenGardenTypes",
                columns: table => new
                {
                    GardenTypesId = table.Column<int>(type: "INTEGER", nullable: false),
                    GardensId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GardenGardenTypes", x => new { x.GardenTypesId, x.GardensId });
                    table.ForeignKey(
                        name: "FK_GardenGardenTypes_GardenTypes_GardenTypesId",
                        column: x => x.GardenTypesId,
                        principalTable: "GardenTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GardenMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GardenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GardenMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GardenMembers_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GardenMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Gardens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gardens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GardenTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GardenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GardenBedId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PlantingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TaskType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GardenTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GardenTasks_GardenBeds_GardenBedId",
                        column: x => x.GardenBedId,
                        principalTable: "GardenBeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GardenTasks_Gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "Gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GardenTasks_Plantings_PlantingId",
                        column: x => x.PlantingId,
                        principalTable: "Plantings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GardenTasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Households",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OwnedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeatherStationIntegrationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Households", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Households_Users_OwnedByUserId",
                        column: x => x.OwnedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GardenId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GardenBedId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InsightType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInsights_GardenBeds_GardenBedId",
                        column: x => x.GardenBedId,
                        principalTable: "GardenBeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserInsights_Gardens_GardenId",
                        column: x => x.GardenId,
                        principalTable: "Gardens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserInsights_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeatherObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TemperatureF = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Humidity = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WindSpeedMph = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    WindDirectionDegrees = table.Column<int>(type: "INTEGER", nullable: true),
                    PrecipitationRateInPerHr = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    PrecipitationTotalIn = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    UvIndex = table.Column<decimal>(type: "decimal(4,1)", nullable: true),
                    DewPointF = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    PressureInHg = table.Column<decimal>(type: "decimal(6,3)", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    StationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherObservations_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeatherStationIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    StationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherStationIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherStationIntegrations_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "GardenTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Vegetable" },
                    { 2, "Fruit" },
                    { 3, "Herb" },
                    { 4, "Flower" },
                    { 5, "Orchard" },
                    { 6, "Greenhouse" },
                    { 7, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AmendmentLogs_AppliedAt",
                table: "AmendmentLogs",
                column: "AppliedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AmendmentLogs_GardenBedId",
                table: "AmendmentLogs",
                column: "GardenBedId");

            migrationBuilder.CreateIndex(
                name: "IX_AmendmentLogs_PlantingId",
                table: "AmendmentLogs",
                column: "PlantingId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenBeds_GardenId",
                table: "GardenBeds",
                column: "GardenId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenGardenTypes_GardensId",
                table: "GardenGardenTypes",
                column: "GardensId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenMembers_InvitedByUserId",
                table: "GardenMembers",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenMembers_UserId",
                table: "GardenMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_GardenMembers_GardenUser",
                table: "GardenMembers",
                columns: new[] { "GardenId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gardens_HouseholdId",
                table: "Gardens",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenTasks_AssignedToUserId",
                table: "GardenTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenTasks_CompletedAt",
                table: "GardenTasks",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GardenTasks_DueDate",
                table: "GardenTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_GardenTasks_GardenBedId",
                table: "GardenTasks",
                column: "GardenBedId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenTasks_GardenId",
                table: "GardenTasks",
                column: "GardenId");

            migrationBuilder.CreateIndex(
                name: "IX_GardenTasks_PlantingId",
                table: "GardenTasks",
                column: "PlantingId");

            migrationBuilder.CreateIndex(
                name: "UQ_GardenTypes_Name",
                table: "GardenTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HarvestLogs_HarvestDate",
                table: "HarvestLogs",
                column: "HarvestDate");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestLogs_HarvestedByUserId",
                table: "HarvestLogs",
                column: "HarvestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestLogs_PlantingId",
                table: "HarvestLogs",
                column: "PlantingId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMembers_UserId",
                table: "HouseholdMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_HouseholdMembers_HouseholdUser",
                table: "HouseholdMembers",
                columns: new[] { "HouseholdId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Households_OwnedByUserId",
                table: "Households",
                column: "OwnedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Households_WeatherStationIntegrationId",
                table: "Households",
                column: "WeatherStationIntegrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_ExpiresAt",
                table: "PasswordResetTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Token",
                table: "PasswordResetTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PestDiseaseLogs_GardenBedId",
                table: "PestDiseaseLogs",
                column: "GardenBedId");

            migrationBuilder.CreateIndex(
                name: "IX_PestDiseaseLogs_PlantingId",
                table: "PestDiseaseLogs",
                column: "PlantingId");

            migrationBuilder.CreateIndex(
                name: "IX_PestDiseaseLogs_ResolvedAt",
                table: "PestDiseaseLogs",
                column: "ResolvedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlantCompanions_CompanionPlantId",
                table: "PlantCompanions",
                column: "CompanionPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantingObservations_ObservedAt",
                table: "PlantingObservations",
                column: "ObservedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlantingObservations_PlantingId",
                table: "PlantingObservations",
                column: "PlantingId");

            migrationBuilder.CreateIndex(
                name: "IX_Plantings_GardenBedId",
                table: "Plantings",
                column: "GardenBedId");

            migrationBuilder.CreateIndex(
                name: "IX_Plantings_PlantId",
                table: "Plantings",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Plantings_Season",
                table: "Plantings",
                columns: new[] { "SeasonYear", "SeasonType" });

            migrationBuilder.CreateIndex(
                name: "IX_Plantings_Status",
                table: "Plantings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_CommonName",
                table: "Plants",
                column: "CommonName");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_ContributedByUserId",
                table: "Plants",
                column: "ContributedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_ExternalSource",
                table: "Plants",
                column: "ExternalSource");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_Family",
                table: "Plants",
                column: "Family");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_IsGlobal",
                table: "Plants",
                columns: new[] { "IsGlobal", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_SoilTests_GardenBedId",
                table: "SoilTests",
                column: "GardenBedId");

            migrationBuilder.CreateIndex(
                name: "IX_SoilTests_TestedAt",
                table: "SoilTests",
                column: "TestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserInsights_ExpiresAt",
                table: "UserInsights",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserInsights_GardenBedId",
                table: "UserInsights",
                column: "GardenBedId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInsights_GardenId",
                table: "UserInsights",
                column: "GardenId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInsights_HouseholdId",
                table: "UserInsights",
                columns: new[] { "HouseholdId", "IsRead", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_Token",
                table: "UserRefreshTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId_Active",
                table: "UserRefreshTokens",
                columns: new[] { "UserId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherObservations_HouseholdId_ObservedAt",
                table: "WeatherObservations",
                columns: new[] { "HouseholdId", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherObservations_ObservedAt",
                table: "WeatherObservations",
                column: "ObservedAt");

            migrationBuilder.CreateIndex(
                name: "UQ_WeatherStationIntegrations_HouseholdId",
                table: "WeatherStationIntegrations",
                column: "HouseholdId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AmendmentLogs_GardenBeds_GardenBedId",
                table: "AmendmentLogs",
                column: "GardenBedId",
                principalTable: "GardenBeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AmendmentLogs_Plantings_PlantingId",
                table: "AmendmentLogs",
                column: "PlantingId",
                principalTable: "Plantings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GardenBeds_Gardens_GardenId",
                table: "GardenBeds",
                column: "GardenId",
                principalTable: "Gardens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GardenGardenTypes_Gardens_GardensId",
                table: "GardenGardenTypes",
                column: "GardensId",
                principalTable: "Gardens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GardenMembers_Gardens_GardenId",
                table: "GardenMembers",
                column: "GardenId",
                principalTable: "Gardens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Gardens_Households_HouseholdId",
                table: "Gardens",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdMembers_Households_HouseholdId",
                table: "HouseholdMembers",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Households_WeatherStationIntegrations_WeatherStationIntegrationId",
                table: "Households",
                column: "WeatherStationIntegrationId",
                principalTable: "WeatherStationIntegrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Households_Users_OwnedByUserId",
                table: "Households");

            migrationBuilder.DropForeignKey(
                name: "FK_WeatherStationIntegrations_Households_HouseholdId",
                table: "WeatherStationIntegrations");

            migrationBuilder.DropTable(
                name: "AmendmentLogs");

            migrationBuilder.DropTable(
                name: "GardenGardenTypes");

            migrationBuilder.DropTable(
                name: "GardenMembers");

            migrationBuilder.DropTable(
                name: "GardenTasks");

            migrationBuilder.DropTable(
                name: "HarvestLogs");

            migrationBuilder.DropTable(
                name: "HouseholdMembers");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PestDiseaseLogs");

            migrationBuilder.DropTable(
                name: "PlantCompanions");

            migrationBuilder.DropTable(
                name: "PlantingObservations");

            migrationBuilder.DropTable(
                name: "SoilTests");

            migrationBuilder.DropTable(
                name: "UserInsights");

            migrationBuilder.DropTable(
                name: "UserRefreshTokens");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "WeatherObservations");

            migrationBuilder.DropTable(
                name: "GardenTypes");

            migrationBuilder.DropTable(
                name: "Plantings");

            migrationBuilder.DropTable(
                name: "GardenBeds");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropTable(
                name: "Gardens");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Households");

            migrationBuilder.DropTable(
                name: "WeatherStationIntegrations");
        }
    }
}
