# Garden Companion — Full Project Context

Garden Companion is a hobby gardening tracker. This skill gives full orientation for any task in the project.

---

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 minimal APIs, MediatR 12, EF Core 9 (SQLite), FluentValidation |
| Frontend | React 18 + Vite + TypeScript, MUI v6, TanStack React Query v5, React Router v6, Axios |
| Auth | JWT (access token) + refresh token (hashed in DB), stored in `localStorage` on client |
| Architecture | Vertical slice CQRS — one `.cs` file per operation |

---

## Solution Structure

```
garden-companion/
  src/GardenCompanion.Api/
    Common/
      GardenAccess.cs             ← RequireMemberAsync, RequireOwnerAsync, RequireBedMemberAsync
      HouseholdAccess.cs
      ClaimsPrincipalExtensions.cs  ← GetUserId() reads "sub" claim
      TokenService.cs
      JwtSettings.cs
    Domain/
      Entities/                   ← EF Core entity classes
      Enums/                      ← All domain enumerations
    Features/                     ← Vertical slices, one file per operation
      Auth/                       RegisterUser, LoginUser, RefreshToken, ForgotPassword, ResetPassword
      Gardens/                    CreateGarden, GetGardens, GetGarden, UpdateGarden, DeleteGarden
      GardenBeds/                 CreateGardenBed, GetGardenBed, UpdateGardenBed, DeleteGardenBed
      GardenMembers/              AddGardenMember (by email), RemoveGardenMember
      GardenTasks/                CreateGardenTask, GetGardenTasks, UpdateGardenTask, CompleteGardenTask, DeleteGardenTask
      SoilTests/                  CreateSoilTest, GetSoilTest, GetSoilTests
      Plants/                     CreatePlant, GetPlant, SearchPlants, PlantCompanions
      Plantings/                  CreatePlanting, GetPlanting, GetPlantings, UpdatePlanting, UpdatePlantingStatus, DeletePlanting
      PlantingObservations/       AddPlantingObservation, GetPlantingObservations
      HarvestLogs/                LogHarvest, GetHarvestLogs
      PestDiseaseLogs/            LogPestDisease, GetPestDiseaseLogs, ResolvePestDiseaseLog
      AmendmentLogs/              LogAmendment, GetAmendmentLogs
      WeatherObservations/        LogWeatherObservation, GetWeatherObservations
      UserInsights/               CreateUserInsight, GetUserInsights, MarkInsightRead
      Users/                      GetMyProfile, UpdateMyProfile, GetMySettings, UpdateMySettings
      Households/                 GetHousehold, UpdateHousehold, AddHouseholdMember, RemoveHouseholdMember,
                                  UpsertWeatherStation, GetWeatherStation, DeleteWeatherStation
    Infrastructure/Data/
      AppDbContext.cs
      Migrations/
    Program.cs                    ← All endpoints registered here under /api MapGroup
  tests/GardenCompanion.Api.Tests/
    Infrastructure/               ← SqliteTestDb, TestApiFactory, TestAuthHandler, FakeEmailService, TestTokenServiceFactory
    Features/                     ← Handler unit tests and endpoint integration tests
  frontend/src/
    api/
      client.ts                   ← Axios instance with JWT auth + 401 refresh retry
      auth.ts
    (Phase 3 — to be built)
```

---

## API Routes (all under `/api`)

```
POST/GET           /api/gardens
GET/PUT/DELETE     /api/gardens/{id}
POST/GET           /api/gardens/{id}/beds
GET/PUT/DELETE     /api/gardens/{id}/beds/{bedId}
POST/DELETE        /api/gardens/{id}/members
GET/POST           /api/gardens/{id}/tasks
PUT/DELETE         /api/gardens/{id}/tasks/{taskId}
POST               /api/gardens/{id}/tasks/{taskId}/complete
GET/POST           /api/gardens/{id}/beds/{bedId}/soil-tests
GET/POST           /api/gardens/{id}/beds/{bedId}/plantings
GET/PUT/DELETE     /api/gardens/{id}/beds/{bedId}/plantings/{plantingId}
POST               /api/gardens/{id}/beds/{bedId}/plantings/{plantingId}/status
GET/POST           /api/gardens/{id}/beds/{bedId}/plantings/{plantingId}/observations
GET/POST           /api/gardens/{id}/beds/{bedId}/plantings/{plantingId}/harvest-logs
GET/POST           /api/gardens/{id}/beds/{bedId}/plantings/{plantingId}/pest-disease-logs
PUT                /api/gardens/{id}/beds/{bedId}/plantings/{plantingId}/pest-disease-logs/{logId}/resolve
GET/POST           /api/gardens/{id}/beds/{bedId}/amendment-logs
GET/POST           /api/weather-observations
GET/POST           /api/user-insights
PATCH              /api/user-insights/{id}/read
GET/PUT            /api/me
GET/PUT            /api/me/settings
GET/PUT            /api/household
POST/DELETE        /api/household/members
GET/PUT/DELETE     /api/household/weather-station
GET/POST           /api/plants
GET                /api/plants/{id}
GET                /api/plants/{id}/companions
POST               /api/auth/register
POST               /api/auth/login
POST               /api/auth/refresh
POST               /api/auth/forgot-password
POST               /api/auth/reset-password
```

---

## Key EF Core Notes

- **Soft delete on Plantings**: global query filter `p.DeletedAt == null` — never add manual where clause
- **PlantCompanion**: self-referencing; Cascade on `PlantId`, Restrict on `CompanionPlantId`
- **Household ↔ WeatherStationIntegration** circular FK: SetNull
- **GardenTask.GardenBedId**: SetNull on bed delete (task becomes garden-wide)
- **GardenBed delete**: blocked by Restrict FK if active Plantings exist → 409
- **GardenType seeded**: Vegetable, Fruit, Herb, Flower, Orchard, Greenhouse, Other
- **Refresh tokens**: hashed in DB (migration: `HashRefreshTokens`)

---

## Authorization Rules

- `KeyNotFoundException` → 404 (also used for "not a member" — prevents info leakage)
- `UnauthorizedAccessException` → 403
- `InvalidOperationException` → 409
- Owner-only: DeleteGarden, AddGardenMember, RemoveGardenMember (except self-remove)
- All other garden ops: any GardenMember OR HouseholdMember of the garden's household

---

## Frontend Auth

- `accessToken` and `refreshToken` stored in `localStorage`
- `client.ts` attaches Bearer token on every request and auto-retries once on 401 using refresh token
- On refresh failure: clears tokens and redirects to `/login`
- API base URL: `http://localhost:5012` (override with `VITE_API_URL` env var)

---

## Tests

- xUnit + FluentAssertions
- Two patterns: handler unit tests (real SQLite `AppDbContext`) and endpoint integration tests (`TestApiFactory` HTTP client)
- `TestAuthHandler` injects a fake authenticated user — set `UserId` in factory for per-test identity
- 13 tests passing as of 2026-04-22
- Phase 1 coverage: Auth (RegisterUser, RefreshToken), Plantings (CreatePlanting), GardenTasks (CreateGardenTask), Gardens (GetGarden)

---

## Frontend Phase 3 — What's next

The frontend is a blank Vite scaffold. Phase 3 builds:
1. MUI theme setup (`/garden-frontend` skill)
2. Dashboard (weather strip, gardens grid, needs-attention insights)
3. Garden detail page (beds, plantings, tasks)
4. Auth flows (login, register, forgot password)

Use `/garden-frontend` for all frontend work and `/new-slice` for any new backend operations.
