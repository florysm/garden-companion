# Technical Debt & Refactoring Guide

Last updated: 2026-04-25. Prioritized by impact on maintainability. Fix opportunistically alongside whichever feature touches the relevant file.

<!-- Scan run 2026-04-25: 0 new items, 1 resolved (frontend #10 submit buttons), several scope counts updated -->

---

## Architecture Decisions

### Plant Data Integration

**Pattern:** Scrapers run on-demand during plant search; data is only committed to the database when a user adds a plant to their garden.

```
GET /api/plants?q=jalapeño
  → SearchPlantsHandler:
      1. Query local DB
      2. If local results < threshold, call IPlantDataService.SearchAsync()
      3. Return merged list — local plants have a real Guid,
         external hits carry ExternalId + ExternalSource (no DB write yet)

POST /api/plantings  { externalPlantId: "...", externalSource: "Scraped" }
  → CreatePlantingHandler:
      1. externalPlantId present → call IPlantDataService.GetAsync(externalPlantId)
      2. Upsert plant into DB (INSERT IF NOT EXISTS to handle concurrent requests)
      3. Create planting linked to the now-real PlantId
```

**Why:** Prevents the DB filling with scraped plants nobody uses. The scraper infrastructure (`IPlantDataService`) stays as a swappable abstraction — new scrapers implement the interface and are registered in `Program.cs`.

---

## Backend

### High Priority

**1. Standardize error response bodies**

`KeyNotFoundException` is caught inconsistently across 41 endpoints:
- 30 return bare `Results.NotFound()` — no detail (mostly GET/DELETE operations)
- 11 return `Results.NotFound(new { error = ex.Message })` — includes detail (mostly POST/PUT operations)

Convention to adopt: always include `new { error = ex.Message }`. This makes client error handling predictable and avoids silent 404s that are hard to debug.

Affected endpoints: all `catch (KeyNotFoundException)` blocks across `Features/`.

---

**2. Extract `PlantingDetailDto` projection**

The same LINQ `Select` projection is copy-pasted in two handlers:
- [GetPlanting.cs](src/GardenCompanion.Api/Features/Plantings/GetPlanting.cs)
- [UpdatePlanting.cs](src/GardenCompanion.Api/Features/Plantings/UpdatePlanting.cs)

`CreatePlanting.cs` constructs the same DTO via manual property assignment rather than a reusable Select, so any DTO change requires updating three files in total.

Extract to a `static IQueryable<PlantingDetailDto> ProjectToDetail(this IQueryable<Planting> query)` extension method in [PlantingDtos.cs](src/GardenCompanion.Api/Features/Plantings/PlantingDtos.cs).

---

**3. Add pagination to list endpoints**

`GetHarvestLogs`, `GetPlantings`, `GetPestDiseaseLogs`, and `GetAmendmentLogs` now have `limit` parameters. These two still return unbounded results:
- `GetGardenTasks` — no limit
- `GetUserInsights` — no limit

Add a shared `PagedRequest` (skip, take) and `PagedResponse<T>` (items, totalCount) in `Common/` and wire up the remaining handlers. Default page size: 50.

---

### Medium Priority

**4. Consolidate DTO files**

Some features embed DTOs inline in the handler file; others use a dedicated `[Feature]Dtos.cs`. Standardize: all DTOs live in `[Feature]Dtos.cs`. Currently inline:
- `HarvestLogs/LogHarvest.cs` — `HarvestLogDto` defined inside handler file
- `AmendmentLogs/LogAmendment.cs` — same pattern
- `PlantingObservations/AddPlantingObservation.cs` — same pattern

---

**5. Centralize enum validation**

Seven endpoints manually parse enums before FluentValidation runs:
- [CreateGardenBed.cs](src/GardenCompanion.Api/Features/GardenBeds/CreateGardenBed.cs)
- [UpdateGardenBed.cs](src/GardenCompanion.Api/Features/GardenBeds/UpdateGardenBed.cs)
- [CreateGardenTask.cs](src/GardenCompanion.Api/Features/GardenTasks/CreateGardenTask.cs)
- [UpdateGardenTask.cs](src/GardenCompanion.Api/Features/GardenTasks/UpdateGardenTask.cs)
- [GetGardenTasks.cs](src/GardenCompanion.Api/Features/GardenTasks/GetGardenTasks.cs)
- [AddGardenMember.cs](src/GardenCompanion.Api/Features/GardenMembers/AddGardenMember.cs)
- [CreateSoilTest.cs](src/GardenCompanion.Api/Features/SoilTests/CreateSoilTest.cs)

Move into FluentValidation rules (`.Must(v => Enum.TryParse<T>(v, true, out _)).WithMessage(...)`) so validation is uniform and consistent with all other field validation.

---

**6. Standardize `UnauthorizedAccessException` responses**

Some handlers return `Results.Forbid()`, others `Results.Problem(ex.Message, statusCode: 403)`. Standardize to `Results.Forbid()` everywhere.

---

### Lower Priority

**7. Authorization chain repetition**

33 handlers repeat the same `GardenAccess.Require*Async` call pattern at the top of `Handle()`. The pattern itself is fine — `GardenAccess` is already a shared helper. Worth noting if the number of features grows significantly; a base handler class could centralize this.

---

## Frontend

### High Priority

**1. `formatDate` + date helpers duplicated**

`formatDate` (with inconsistent `month: 'short'` vs `month: 'long'`) is copy-pasted across 8 files. `today()` / `todayIso()` are defined in 5 separate files.

Extract all to `src/utils/dateUtils.ts`:
```ts
export function formatDate(iso: string): string { ... }
export function todayIso(): string { ... }
```

Affected: `GardenBedDetailPage`, `PlantingDetailPage`, `AmendmentLog`, `PestDiseaseLog`, `SoilTestLog`, `HarvestLog`, `ObservationLog`, `CreatePlantingPage`

---

**2. `ChipGroup` component duplicated**

Identical inline chip-select component defined at `CreateGardenBedPage.tsx:44` and `EditPlantingPage.tsx:39`. Extract to `src/components/common/ChipSelect.tsx`.

---

**3. Chip select `sx` styling duplicated**

Same `bgcolor`/`borderColor`/`hover` style object appears in 8 page files. Extract to `src/utils/chipStyles.ts`:
```ts
export const chipSelectSx = (selected: boolean) => ({ ... })
```

---

**4. Hardcoded option arrays in create/edit pairs**

Task type arrays, planting types, planting sources, and season type arrays are defined independently in each create page and its matching edit page. They drift apart over time. Move to `src/constants/gardenOptions.ts` (or per-domain files under `src/constants/`).

---

**5. `useDebounce` is inline**

Defined at `CreatePlantingPage.tsx:49`. Move to `src/hooks/useDebounce.ts` before any second debounced search field is added.

---

### Medium Priority

**6. Log component toggle pattern repeated**

`HarvestLog`, `ObservationLog`, `AmendmentLog`, `PestDiseaseLog`, and `SoilTestLog` all implement the same pattern: `adding` boolean state, conditional inline form, reset on success/cancel. Extract to an `ExpandableLogSection` component that accepts form content as children or a render prop.

---

**7. `initialized` form seeding pattern**

Five components use a manual `initialized` / `nameInitialized` flag to one-time seed form state from async data. Goes stale if the query refetches with new data.

- `EditGardenPage.tsx:41`
- `EditPlantingPage.tsx:99`
- `EditGardenTaskPage.tsx:60`
- `SettingsPage.tsx:50` (ProfileSection `nameInitialized`)
- `SettingsPage.tsx:241` (HouseholdSection `nameInitialized`)

Refactor: remove the flag and call a `reset(data)` inside `useEffect` whenever the query result changes identity.

---

**8. Status chip style mappings duplicated**

`GardenBedDetailPage.tsx:20` and `PlantingDetailPage.tsx:19` have near-identical status→chip style maps. Extract to `src/utils/plantingStatus.ts`.

---

### Lower Priority

**9. No global error toast**

Mutation `onError` callbacks currently do nothing or show inline text inconsistently. Add an MUI `Snackbar` provider in `App.tsx` and a `useErrorToast` hook for consistent failure feedback.

**10. Query key naming inconsistency**

Mix of singular/plural keys (`['garden', id]` vs `['gardens']`) and naming styles (`['pestDisease', ...]` camelCase vs `['amendments', ...]` lowercase) means some invalidations may miss related caches. Audit and standardize; consider a `queryKeys` factory object.

**11. No React error boundary**

Unhandled component exceptions crash the entire app to a blank white screen. Add an `<ErrorBoundary>` wrapper in `App.tsx`.

**12. No 404 page**

Unknown routes silently redirect to `/` with no user feedback. Add a `NotFoundPage` and point the catch-all `path="*"` route to it.

---

## Not Worth Doing (Yet)

- **Repository pattern** — direct DbContext queries in handlers are clean enough; adding a repo layer at this scale adds indirection with no benefit
- **Domain events** — premature for a hobby app; add if background jobs or cross-aggregate side effects become a real need
- **Result<T> error wrapper** — exception-based error handling is consistent throughout; the churn to switch isn't justified
- **Audit fields** (CreatedBy/UpdatedBy) — no current feature depends on this; add if an audit trail becomes a user-facing requirement
