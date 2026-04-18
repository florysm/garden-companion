# Garden Companion App — Database Schema

## Design Principles

- All primary keys are `UNIQUEIDENTIFIER` (Guid) unless noted
- All timestamps are `DATETIME2` stored in UTC
- Dates without time (planting dates, harvest dates) use `DATE`
- Decimal measurements stored in base imperial units; conversion happens at the API layer
- Soft deletes via `DeletedAt` on `Planting` only — all other entities use hard delete
- Encrypted fields (API keys) handled at the application layer before persistence

---

## The Tuesday Evening Test

> *"Would an enthusiastic home gardener actually use this on a Tuesday evening after work?"*

Every field in this schema passed this test. If it's here, it earns its place.

---

## Domain 1 — Identity & Sharing

### User
```sql
CREATE TABLE Users (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    Email               NVARCHAR(256)       NOT NULL,
    PasswordHash        NVARCHAR(512)       NOT NULL,
    DisplayName         NVARCHAR(100)       NOT NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

CREATE INDEX IX_Users_Email ON Users (Email);
```

### UserSettings
One-to-one with User. Stores localization preferences and data sharing consent inline.

```sql
CREATE TABLE UserSettings (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId                  UNIQUEIDENTIFIER    NOT NULL,
    LocationLatitude        DECIMAL(9,6)        NULL,
    LocationLongitude       DECIMAL(9,6)        NULL,
    PreferredLanguage       NVARCHAR(10)        NOT NULL DEFAULT 'en-US',   -- IETF language tag
    TemperatureUnit         NVARCHAR(20)        NOT NULL DEFAULT 'Fahrenheit',  -- Fahrenheit | Celsius
    LengthUnit              NVARCHAR(20)        NOT NULL DEFAULT 'Inches',      -- Inches | Centimeters | Feet | Meters
    WeightUnit              NVARCHAR(20)        NOT NULL DEFAULT 'Pounds',      -- Pounds | Kilograms | Ounces | Grams
    VolumeUnit              NVARCHAR(20)        NOT NULL DEFAULT 'Gallons',     -- Gallons | Liters
    UsdaHardinessZone       NVARCHAR(10)        NULL,   -- e.g. '6b'
    AverageFrostDateSpring  DATE                NULL,
    AverageFrostDateFall    DATE                NULL,
    ShareWeatherData        BIT                 NOT NULL DEFAULT 0,
    SharePlantingData       BIT                 NOT NULL DEFAULT 0,
    ShareHarvestData        BIT                 NOT NULL DEFAULT 0,

    CONSTRAINT PK_UserSettings PRIMARY KEY (Id),
    CONSTRAINT FK_UserSettings_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_UserSettings_UserId UNIQUE (UserId)
);
```

### PasswordResetTokens
Short-lived tokens for forgot/reset password flow. Token stored hashed — never plain text.

```sql
CREATE TABLE PasswordResetTokens (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId      UNIQUEIDENTIFIER    NOT NULL,
    Token       NVARCHAR(512)       NOT NULL,  -- hashed at application layer
    ExpiresAt   DATETIME2           NOT NULL,
    UsedAt      DATETIME2           NULL,      -- null = unused, set on redemption

    CONSTRAINT PK_PasswordResetTokens PRIMARY KEY (Id),
    CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_PasswordResetTokens_Token ON PasswordResetTokens (Token);
CREATE INDEX IX_PasswordResetTokens_ExpiresAt ON PasswordResetTokens (ExpiresAt);
```

> **Security note:** Tokens expire after 1 hour. `UsedAt` prevents replay attacks — a token
> can only be redeemed once. A background job should purge expired and used tokens daily.

### Household
The sharing unit. A solo user is a household of one.

```sql
CREATE TABLE Households (
    Id                              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    Name                            NVARCHAR(100)       NOT NULL,
    OwnedByUserId                   UNIQUEIDENTIFIER    NOT NULL,
    WeatherStationIntegrationId     UNIQUEIDENTIFIER    NULL,   -- FK added after WeatherStationIntegrations
    CreatedAt                       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Households PRIMARY KEY (Id),
    CONSTRAINT FK_Households_Users FOREIGN KEY (OwnedByUserId) REFERENCES Users(Id)
);

CREATE INDEX IX_Households_OwnedByUserId ON Households (OwnedByUserId);
```

### HouseholdMember
```sql
CREATE TABLE HouseholdMembers (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    HouseholdId     UNIQUEIDENTIFIER    NOT NULL,
    UserId          UNIQUEIDENTIFIER    NOT NULL,
    Role            NVARCHAR(20)        NOT NULL DEFAULT 'Contributor',  -- Owner | Contributor
    JoinedAt        DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_HouseholdMembers PRIMARY KEY (Id),
    CONSTRAINT FK_HouseholdMembers_Households FOREIGN KEY (HouseholdId) REFERENCES Households(Id) ON DELETE CASCADE,
    CONSTRAINT FK_HouseholdMembers_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UQ_HouseholdMembers_HouseholdUser UNIQUE (HouseholdId, UserId)
);

CREATE INDEX IX_HouseholdMembers_UserId ON HouseholdMembers (UserId);
```

### WeatherStationIntegration
One per household. Null StationId/ApiKey means Open-Meteo fallback.

```sql
CREATE TABLE WeatherStationIntegrations (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    HouseholdId     UNIQUEIDENTIFIER    NOT NULL,
    Provider        NVARCHAR(30)        NOT NULL,  -- WeatherUnderground | AmbientWeather | WeatherFlowTempest | DavisWeatherLink | OpenMeteo
    StationId       NVARCHAR(100)       NULL,
    ApiKey          NVARCHAR(512)       NULL,      -- encrypted at application layer
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_WeatherStationIntegrations PRIMARY KEY (Id),
    CONSTRAINT FK_WeatherStationIntegrations_Households FOREIGN KEY (HouseholdId) REFERENCES Households(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_WeatherStationIntegrations_HouseholdId UNIQUE (HouseholdId)
);

-- Add FK back to Households now that WeatherStationIntegrations exists
ALTER TABLE Households
    ADD CONSTRAINT FK_Households_WeatherStationIntegrations
    FOREIGN KEY (WeatherStationIntegrationId)
    REFERENCES WeatherStationIntegrations(Id)
    ON DELETE SET NULL;
```

---

## Domain 2 — Garden & Beds

### Garden
```sql
CREATE TABLE Gardens (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    HouseholdId     UNIQUEIDENTIFIER    NOT NULL,
    Name            NVARCHAR(100)       NOT NULL,
    Description     NVARCHAR(500)       NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Gardens PRIMARY KEY (Id),
    CONSTRAINT FK_Gardens_Households FOREIGN KEY (HouseholdId) REFERENCES Households(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Gardens_HouseholdId ON Gardens (HouseholdId);
```

### GardenType
Lookup table — seeded at migration time.

```sql
CREATE TABLE GardenTypes (
    Id      INT             NOT NULL IDENTITY(1,1),
    Name    NVARCHAR(50)    NOT NULL,

    CONSTRAINT PK_GardenTypes PRIMARY KEY (Id),
    CONSTRAINT UQ_GardenTypes_Name UNIQUE (Name)
);

-- Seed data
INSERT INTO GardenTypes (Name) VALUES
    ('Vegetable'), ('Fruit'), ('Herb'), ('Flower'), ('Orchard'), ('Greenhouse'), ('Other');
```

### GardenGardenType
Many-to-many join — a garden can have multiple types.

```sql
CREATE TABLE GardenGardenTypes (
    GardenId        UNIQUEIDENTIFIER    NOT NULL,
    GardenTypeId    INT                 NOT NULL,

    CONSTRAINT PK_GardenGardenTypes PRIMARY KEY (GardenId, GardenTypeId),
    CONSTRAINT FK_GardenGardenTypes_Gardens FOREIGN KEY (GardenId) REFERENCES Gardens(Id) ON DELETE CASCADE,
    CONSTRAINT FK_GardenGardenTypes_GardenTypes FOREIGN KEY (GardenTypeId) REFERENCES GardenTypes(Id)
);
```

### GardenMember
Per-garden access control — Owner or Contributor.

```sql
CREATE TABLE GardenMembers (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    GardenId            UNIQUEIDENTIFIER    NOT NULL,
    UserId              UNIQUEIDENTIFIER    NOT NULL,
    Role                NVARCHAR(20)        NOT NULL DEFAULT 'Contributor',  -- Owner | Contributor
    InvitedByUserId     UNIQUEIDENTIFIER    NULL,

    CONSTRAINT PK_GardenMembers PRIMARY KEY (Id),
    CONSTRAINT FK_GardenMembers_Gardens FOREIGN KEY (GardenId) REFERENCES Gardens(Id) ON DELETE CASCADE,
    CONSTRAINT FK_GardenMembers_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_GardenMembers_InvitedBy FOREIGN KEY (InvitedByUserId) REFERENCES Users(Id),
    CONSTRAINT UQ_GardenMembers_GardenUser UNIQUE (GardenId, UserId)
);

CREATE INDEX IX_GardenMembers_UserId ON GardenMembers (UserId);
```

### GardenBed
Core physical entity. Dimensions are nullable because they depend on shape.

```sql
CREATE TABLE GardenBeds (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    GardenId        UNIQUEIDENTIFIER    NOT NULL,
    Name            NVARCHAR(100)       NOT NULL,
    Type            NVARCHAR(30)        NOT NULL,   -- InGround | RaisedGround | RaisedSupported | Container | VerticalPlanter | StrawBale
    Shape           NVARCHAR(20)        NOT NULL,   -- Rectangle | Square | Round | Oval | LShaped | Triangle | FreeForm
    LengthFeet      DECIMAL(6,2)        NULL,       -- Rectangle, Square, LShaped, Triangle
    WidthFeet       DECIMAL(6,2)        NULL,       -- Rectangle, Square, LShaped, Triangle
    DiameterFeet    DECIMAL(6,2)        NULL,       -- Round
    DepthInches     DECIMAL(6,2)        NULL,       -- Raised and Container types
    VolumeGallons   DECIMAL(8,2)        NULL,       -- Container type
    SoilType        NVARCHAR(100)       NULL,
    SunExposure     NVARCHAR(20)        NOT NULL DEFAULT 'FullSun',  -- FullSun | PartialShade | FullShade
    Notes           NVARCHAR(1000)      NULL,

    CONSTRAINT PK_GardenBeds PRIMARY KEY (Id),
    CONSTRAINT FK_GardenBeds_Gardens FOREIGN KEY (GardenId) REFERENCES Gardens(Id) ON DELETE CASCADE
);

CREATE INDEX IX_GardenBeds_GardenId ON GardenBeds (GardenId);
```

### SoilTest
Optional. Multiple tests per bed build a health history over time.

```sql
CREATE TABLE SoilTests (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    GardenBedId             UNIQUEIDENTIFIER    NOT NULL,
    TestedAt                DATE                NOT NULL,
    PhLevel                 DECIMAL(4,2)        NULL,   -- 0.00 to 14.00
    NitrogenPpm             DECIMAL(8,2)        NULL,
    PhosphorusPpm           DECIMAL(8,2)        NULL,
    PotassiumPpm            DECIMAL(8,2)        NULL,
    OrganicMatterPercent    DECIMAL(5,2)        NULL,
    TestSource              NVARCHAR(20)        NOT NULL DEFAULT 'Manual',  -- HomeKit | LabTest | Manual
    Notes                   NVARCHAR(500)       NULL,
    CreatedAt               DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_SoilTests PRIMARY KEY (Id),
    CONSTRAINT FK_SoilTests_GardenBeds FOREIGN KEY (GardenBedId) REFERENCES GardenBeds(Id) ON DELETE CASCADE
);

CREATE INDEX IX_SoilTests_GardenBedId ON SoilTests (GardenBedId);
CREATE INDEX IX_SoilTests_TestedAt ON SoilTests (TestedAt);
```

### GardenTask
Simple task tracking. No recurrence — just a due date and a completion timestamp.

```sql
CREATE TABLE GardenTasks (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    GardenId            UNIQUEIDENTIFIER    NOT NULL,
    GardenBedId         UNIQUEIDENTIFIER    NULL,   -- null = garden-wide task
    PlantingId          UNIQUEIDENTIFIER    NULL,   -- null = not planting-specific
    AssignedToUserId    UNIQUEIDENTIFIER    NULL,
    Title               NVARCHAR(200)       NOT NULL,
    Description         NVARCHAR(1000)      NULL,
    TaskType            NVARCHAR(20)        NOT NULL DEFAULT 'General',  -- Water | Fertilize | Harvest | Prune | Inspect | Amend | Plant | General
    DueDate             DATE                NULL,
    CompletedAt         DATETIME2           NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_GardenTasks PRIMARY KEY (Id),
    CONSTRAINT FK_GardenTasks_Gardens FOREIGN KEY (GardenId) REFERENCES Gardens(Id) ON DELETE CASCADE,
    CONSTRAINT FK_GardenTasks_GardenBeds FOREIGN KEY (GardenBedId) REFERENCES GardenBeds(Id),
    CONSTRAINT FK_GardenTasks_Users FOREIGN KEY (AssignedToUserId) REFERENCES Users(Id)
);

CREATE INDEX IX_GardenTasks_GardenId ON GardenTasks (GardenId);
CREATE INDEX IX_GardenTasks_DueDate ON GardenTasks (DueDate) WHERE DueDate IS NOT NULL;
CREATE INDEX IX_GardenTasks_CompletedAt ON GardenTasks (CompletedAt) WHERE CompletedAt IS NULL;
```

---

## Domain 3 — Plants & Plantings

### Plant
Cached from external APIs or manually contributed. System manages API keys.

```sql
CREATE TABLE Plants (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    ExternalId              NVARCHAR(100)       NULL,   -- null if Manual
    ExternalSource          NVARCHAR(20)        NOT NULL,  -- Perenual | OpenFarm | Trefle | Manual
    ContributedByUserId     UNIQUEIDENTIFIER    NULL,   -- null if from external API
    IsGlobal                BIT                 NOT NULL DEFAULT 0,
    IsApproved              BIT                 NOT NULL DEFAULT 0,
    CommonName              NVARCHAR(200)       NOT NULL,
    ScientificName          NVARCHAR(200)       NULL,
    Description             NVARCHAR(2000)      NULL,
    DaysToMaturity          INT                 NULL,
    MinSpacingInches        DECIMAL(6,2)        NULL,
    SunRequirement          NVARCHAR(100)       NULL,
    WaterRequirement        NVARCHAR(100)       NULL,
    MinDepthInches          DECIMAL(6,2)        NULL,
    Family                  NVARCHAR(100)       NULL,   -- e.g. 'Nightshade', 'Cucurbit', 'Allium', 'Brassica' — sourced from API or manual
    CachedAt                DATETIME2           NULL,   -- null if Manual

    CONSTRAINT PK_Plants PRIMARY KEY (Id),
    CONSTRAINT FK_Plants_Users FOREIGN KEY (ContributedByUserId) REFERENCES Users(Id)
);

CREATE INDEX IX_Plants_ExternalSource ON Plants (ExternalSource);
CREATE INDEX IX_Plants_CommonName ON Plants (CommonName);
CREATE INDEX IX_Plants_Family ON Plants (Family) WHERE Family IS NOT NULL;
CREATE INDEX IX_Plants_IsGlobal ON Plants (IsGlobal, IsApproved);
```

### PlantCompanion
Self-referencing many-to-many. Sourced from plant APIs or manual entry.

```sql
CREATE TABLE PlantCompanions (
    PlantId             UNIQUEIDENTIFIER    NOT NULL,
    CompanionPlantId    UNIQUEIDENTIFIER    NOT NULL,
    RelationshipType    NVARCHAR(20)        NOT NULL,  -- Beneficial | Harmful

    CONSTRAINT PK_PlantCompanions PRIMARY KEY (PlantId, CompanionPlantId),
    CONSTRAINT FK_PlantCompanions_Plant FOREIGN KEY (PlantId) REFERENCES Plants(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PlantCompanions_CompanionPlant FOREIGN KEY (CompanionPlantId) REFERENCES Plants(Id),
    CONSTRAINT CK_PlantCompanions_NoSelf CHECK (PlantId <> CompanionPlantId)
);
```

### Planting
The core lifecycle entity. Soft deleted via DeletedAt.

```sql
CREATE TABLE Plantings (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    GardenBedId             UNIQUEIDENTIFIER    NOT NULL,
    PlantId                 UNIQUEIDENTIFIER    NOT NULL,
    PlantedDate             DATE                NOT NULL,
    ExpectedHarvestDate     DATE                NULL,   -- derived from PlantedDate + DaysToMaturity
    ActualEndDate           DATE                NULL,
    Status                  NVARCHAR(20)        NOT NULL DEFAULT 'Planted',  -- Planted | Growing | Producing | Harvested | Failed
    PlantingType            NVARCHAR(20)        NOT NULL DEFAULT 'Annual',   -- Annual | Perennial | Biennial
    Quantity                INT                 NOT NULL DEFAULT 1,
    SeasonYear              INT                 NOT NULL,
    SeasonType              NVARCHAR(20)        NOT NULL,  -- Spring | Summer | Fall | Winter | YearRound
    IsActive                BIT                 NOT NULL DEFAULT 1,
    DeletedAt               DATETIME2           NULL,   -- soft delete

    CONSTRAINT PK_Plantings PRIMARY KEY (Id),
    CONSTRAINT FK_Plantings_GardenBeds FOREIGN KEY (GardenBedId) REFERENCES GardenBeds(Id),
    CONSTRAINT FK_Plantings_Plants FOREIGN KEY (PlantId) REFERENCES Plants(Id),
    CONSTRAINT CK_Plantings_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_Plantings_SeasonYear CHECK (SeasonYear >= 2000 AND SeasonYear <= 2100)
);

CREATE INDEX IX_Plantings_GardenBedId ON Plantings (GardenBedId) WHERE DeletedAt IS NULL;
CREATE INDEX IX_Plantings_PlantId ON Plantings (PlantId);
CREATE INDEX IX_Plantings_Status ON Plantings (Status) WHERE DeletedAt IS NULL;
CREATE INDEX IX_Plantings_Season ON Plantings (SeasonYear, SeasonType) WHERE DeletedAt IS NULL;
```

### PlantingObservation
Timestamped freeform notes on a planting.

```sql
CREATE TABLE PlantingObservations (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    PlantingId          UNIQUEIDENTIFIER    NOT NULL,
    ObservationType     NVARCHAR(20)        NOT NULL DEFAULT 'General',  -- General | Pest | Disease | Growth | Fertilized | Watered
    Note                NVARCHAR(2000)      NOT NULL,
    ObservedAt          DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_PlantingObservations PRIMARY KEY (Id),
    CONSTRAINT FK_PlantingObservations_Plantings FOREIGN KEY (PlantingId) REFERENCES Plantings(Id) ON DELETE CASCADE
);

CREATE INDEX IX_PlantingObservations_PlantingId ON PlantingObservations (PlantingId);
CREATE INDEX IX_PlantingObservations_ObservedAt ON PlantingObservations (ObservedAt);
```

### PestDiseaseLog
Structured pest and disease tracking. Can be planting-specific or bed-wide.

```sql
CREATE TABLE PestDiseaseLogs (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    PlantingId          UNIQUEIDENTIFIER    NULL,   -- null = bed-wide event
    GardenBedId         UNIQUEIDENTIFIER    NOT NULL,
    ObservedAt          DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    Type                NVARCHAR(20)        NOT NULL,  -- Pest | Disease | NutrientDeficiency
    Name                NVARCHAR(200)       NOT NULL,  -- e.g. 'Aphids', 'Powdery Mildew'
    Severity            NVARCHAR(10)        NOT NULL DEFAULT 'Low',  -- Low | Medium | High
    TreatmentApplied    NVARCHAR(500)       NULL,
    ResolvedAt          DATETIME2           NULL,
    Notes               NVARCHAR(1000)      NULL,

    CONSTRAINT PK_PestDiseaseLogs PRIMARY KEY (Id),
    CONSTRAINT FK_PestDiseaseLogs_Plantings FOREIGN KEY (PlantingId) REFERENCES Plantings(Id),
    CONSTRAINT FK_PestDiseaseLogs_GardenBeds FOREIGN KEY (GardenBedId) REFERENCES GardenBeds(Id) ON DELETE CASCADE
);

CREATE INDEX IX_PestDiseaseLogs_GardenBedId ON PestDiseaseLogs (GardenBedId);
CREATE INDEX IX_PestDiseaseLogs_PlantingId ON PestDiseaseLogs (PlantingId) WHERE PlantingId IS NOT NULL;
CREATE INDEX IX_PestDiseaseLogs_ResolvedAt ON PestDiseaseLogs (ResolvedAt) WHERE ResolvedAt IS NULL;
```

### AmendmentLog
What was applied to a bed or planting and when.

```sql
CREATE TABLE AmendmentLogs (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    GardenBedId     UNIQUEIDENTIFIER    NOT NULL,
    PlantingId      UNIQUEIDENTIFIER    NULL,   -- null = bed-wide amendment
    AppliedAt       DATE                NOT NULL,
    ProductName     NVARCHAR(200)       NOT NULL,
    AmendmentType   NVARCHAR(30)        NOT NULL,  -- Fertilizer | Compost | Mulch | PhAdjuster | Pesticide | HerbControl | Other
    Quantity        DECIMAL(10,3)       NOT NULL,
    QuantityUnit    NVARCHAR(20)        NOT NULL,  -- Pounds | Ounces | Grams | Kilograms | Gallons | Liters | Milliliters
    Notes           NVARCHAR(500)       NULL,

    CONSTRAINT PK_AmendmentLogs PRIMARY KEY (Id),
    CONSTRAINT FK_AmendmentLogs_GardenBeds FOREIGN KEY (GardenBedId) REFERENCES GardenBeds(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AmendmentLogs_Plantings FOREIGN KEY (PlantingId) REFERENCES Plantings(Id),
    CONSTRAINT CK_AmendmentLogs_Quantity CHECK (Quantity > 0)
);

CREATE INDEX IX_AmendmentLogs_GardenBedId ON AmendmentLogs (GardenBedId);
CREATE INDEX IX_AmendmentLogs_AppliedAt ON AmendmentLogs (AppliedAt);
```

### HarvestLog
What was picked, how much, and by whom.

```sql
CREATE TABLE HarvestLogs (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    PlantingId          UNIQUEIDENTIFIER    NOT NULL,
    HarvestedByUserId   UNIQUEIDENTIFIER    NOT NULL,
    HarvestDate         DATE                NOT NULL,
    Quantity            DECIMAL(10,3)       NOT NULL,
    QuantityUnit        NVARCHAR(20)        NOT NULL,  -- Count | Grams | Ounces | Pounds | Kilograms
    Notes               NVARCHAR(500)       NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_HarvestLogs PRIMARY KEY (Id),
    CONSTRAINT FK_HarvestLogs_Plantings FOREIGN KEY (PlantingId) REFERENCES Plantings(Id) ON DELETE CASCADE,
    CONSTRAINT FK_HarvestLogs_Users FOREIGN KEY (HarvestedByUserId) REFERENCES Users(Id),
    CONSTRAINT CK_HarvestLogs_Quantity CHECK (Quantity > 0)
);

CREATE INDEX IX_HarvestLogs_PlantingId ON HarvestLogs (PlantingId);
CREATE INDEX IX_HarvestLogs_HarvestDate ON HarvestLogs (HarvestDate);
CREATE INDEX IX_HarvestLogs_HarvestedByUserId ON HarvestLogs (HarvestedByUserId);
```

---

## Domain 4 — Weather & Insights

### WeatherObservation
Scoped to Household — shared by all members. Stored in base imperial units.

```sql
CREATE TABLE WeatherObservations (
    Id                          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    HouseholdId                 UNIQUEIDENTIFIER    NOT NULL,
    ObservedAt                  DATETIME2           NOT NULL,
    TemperatureF                DECIMAL(6,2)        NOT NULL,
    Humidity                    DECIMAL(5,2)        NOT NULL,   -- percentage 0-100
    WindSpeedMph                DECIMAL(6,2)        NOT NULL,
    WindDirectionDegrees        INT                 NULL,       -- 0-359
    PrecipitationRateInPerHr    DECIMAL(6,3)        NOT NULL DEFAULT 0,
    PrecipitationTotalIn        DECIMAL(6,3)        NOT NULL DEFAULT 0,
    UvIndex                     DECIMAL(4,1)        NULL,
    DewPointF                   DECIMAL(6,2)        NULL,
    PressureInHg                DECIMAL(6,3)        NULL,
    Source                      NVARCHAR(30)        NOT NULL,   -- WeatherUnderground | AmbientWeather | WeatherFlowTempest | DavisWeatherLink | OpenMeteo
    StationId                   NVARCHAR(100)       NULL,

    CONSTRAINT PK_WeatherObservations PRIMARY KEY (Id),
    CONSTRAINT FK_WeatherObservations_Households FOREIGN KEY (HouseholdId) REFERENCES Households(Id) ON DELETE CASCADE,
    CONSTRAINT CK_WeatherObservations_Humidity CHECK (Humidity >= 0 AND Humidity <= 100),
    CONSTRAINT CK_WeatherObservations_WindDirection CHECK (WindDirectionDegrees IS NULL OR (WindDirectionDegrees >= 0 AND WindDirectionDegrees <= 359))
);

CREATE INDEX IX_WeatherObservations_HouseholdId_ObservedAt ON WeatherObservations (HouseholdId, ObservedAt DESC);
CREATE INDEX IX_WeatherObservations_ObservedAt ON WeatherObservations (ObservedAt DESC);
```

> **Retention note:** Weather observations will grow quickly. Consider a background job to
> aggregate and prune observations older than 90 days down to daily summaries only.

### UserInsight
Generated insights and alerts. Scoped from Household down to GardenBed.

```sql
CREATE TABLE UserInsights (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    HouseholdId         UNIQUEIDENTIFIER    NOT NULL,
    GardenId            UNIQUEIDENTIFIER    NULL,
    GardenBedId         UNIQUEIDENTIFIER    NULL,
    InsightType         NVARCHAR(30)        NOT NULL,  -- YieldTrend | RotationWarning | WeatherAlert | PestWarning | PlantingRecommendation | WateringRecommendation
    Title               NVARCHAR(200)       NOT NULL,
    Body                NVARCHAR(1000)      NOT NULL,
    IsRead              BIT                 NOT NULL DEFAULT 0,
    ExpiresAt           DATETIME2           NULL,
    GeneratedAt         DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_UserInsights PRIMARY KEY (Id),
    CONSTRAINT FK_UserInsights_Households FOREIGN KEY (HouseholdId) REFERENCES Households(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserInsights_Gardens FOREIGN KEY (GardenId) REFERENCES Gardens(Id),
    CONSTRAINT FK_UserInsights_GardenBeds FOREIGN KEY (GardenBedId) REFERENCES GardenBeds(Id)
);

CREATE INDEX IX_UserInsights_HouseholdId ON UserInsights (HouseholdId, IsRead, GeneratedAt DESC);
CREATE INDEX IX_UserInsights_ExpiresAt ON UserInsights (ExpiresAt) WHERE ExpiresAt IS NOT NULL;
```

---

## Summary

| Domain | Table | PK Type | Rows (est. hobby user/yr) |
|---|---|---|---|
| Identity | Users | Guid | 1 |
| Identity | UserSettings | Guid | 1 |
| Identity | PasswordResetTokens | Guid | 0-5 |
| Identity | Households | Guid | 1 |
| Identity | HouseholdMembers | Guid | 1-5 |
| Identity | WeatherStationIntegrations | Guid | 0-1 |
| Garden | Gardens | Guid | 1-5 |
| Garden | GardenTypes | Int | 7 (seeded) |
| Garden | GardenGardenTypes | Composite | 2-10 |
| Garden | GardenMembers | Guid | 0-5 |
| Garden | GardenBeds | Guid | 2-20 |
| Garden | SoilTests | Guid | 0-20 |
| Garden | GardenTasks | Guid | 10-100 |
| Plants | Plants | Guid | Shared/global |
| Plants | PlantCompanions | Composite | Shared/global |
| Plants | Plantings | Guid | 10-100 |
| Plants | PlantingObservations | Guid | 20-200 |
| Plants | PestDiseaseLogs | Guid | 0-50 |
| Plants | AmendmentLogs | Guid | 0-50 |
| Plants | HarvestLogs | Guid | 20-500 |
| Weather | WeatherObservations | Guid | 50,000-100,000 |
| Insights | UserInsights | Guid | 10-100 |

**Total: 22 tables** (GardenTypes is a lookup, not a domain entity)

---

## EF Core Notes for Implementation

- Use `NEWSEQUENTIALID()` default via `ValueGeneratedOnAdd()` — avoids index fragmentation vs random Guids
- Configure all enum-like string columns with `HasConversion<string>()` and `HasMaxLength()`
- `Planting` soft delete via global query filter: `.HasQueryFilter(p => p.DeletedAt == null)`
- `PlantCompanions` self-referencing requires explicit fluent configuration to avoid cascade delete conflicts
- `WeatherObservations` — consider a separate `IDbContext` or bounded context given volume
- All nullable FKs should be configured with `OnDelete(DeleteBehavior.SetNull)` or `Restrict` as appropriate
- `PasswordResetTokens` — hash tokens with SHA-256 before persistence; never store or log plain text tokens

---

## Infrastructure Notes

### Email Service
Required for forgot/reset password flow. Abstract from day one so the provider is swappable:

```
Infrastructure/
└── Email/
    ├── IEmailService.cs           → SendPasswordResetEmail(string to, string resetLink)
    └── SmtpEmailService.cs        → swap for SendGrid / Mailgun / SES later
```

Start with SMTP for local development. The interface is all the application layer touches.

### GetPlantings Query Filters
The `GetPlantings` query supports the following composable filters — all optional:

```
GetPlantingsQuery
├── GardenBedId     (nullable)  → filter by specific bed
├── GardenId        (nullable)  → filter across all beds in a garden
├── SeasonYear      (nullable)  → filter by year
├── SeasonType      (nullable)  → Spring | Summer | Fall | Winter | YearRound
├── Status          (nullable)  → Planted | Growing | Producing | Harvested | Failed
├── PlantFamily     (nullable)  → e.g. 'Nightshade' — joins through Plant.Family
├── PlantId         (nullable)  → specific plant
└── PlantingType    (nullable)  → Annual | Perennial | Biennial
```

`PlantFamily` joins through the `Plants` table on `Family` — enables "show me all my tomatoes
across all beds and seasons" without needing to know specific plant IDs.
