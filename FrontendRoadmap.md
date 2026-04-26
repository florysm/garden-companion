# Frontend Roadmap

Last updated: 2026-04-25. Prioritized by user impact — higher phases assume lower phases are mostly done.

---

## Completed

- [x] Auth flow — login, register, forgot/reset password, token refresh interceptor
- [x] Dashboard — garden grid, skeletons, empty state, Insights "Needs Attention" section
- [x] Gardens — create, detail (beds grid)
- [x] Garden Beds — create, detail (plantings grid with status chips)
- [x] Plantings — create (plant search, auto harvest date), detail (status progression, stat cards)
- [x] Garden Tasks — create, list (Open/Completed), complete (optimistic), delete (optimistic)
- [x] **Edit garden** — `PUT /api/gardens/{id}` · EditGardenPage at `/gardens/:id/edit`
- [x] **Delete garden** — `DELETE /api/gardens/{id}` · ConfirmDeleteDialog on GardenDetailPage (owner only)
- [x] **Edit garden bed** — `PUT /api/gardens/{id}/beds/{bedId}` · EditGardenBedPage at `/gardens/:id/beds/:bedId/edit`
- [x] **Delete garden bed** — `DELETE /api/gardens/{id}/beds/{bedId}` · ConfirmDeleteDialog on GardenBedDetailPage; backend returns 409 if active plantings exist
- [x] **Edit planting** — `PUT /api/plantings/{id}` · EditPlantingPage at `/gardens/:id/plantings/:plantingId/edit`
- [x] **Delete planting** — `DELETE /api/plantings/{id}` · ConfirmDeleteDialog on PlantingDetailPage
- [x] **Edit task** — `PUT /api/gardens/{id}/tasks/{taskId}` · EditGardenTaskPage at `/gardens/:id/tasks/:taskId/edit`
- [x] **Planting observations** — `POST /api/plantings/{id}/observations`, `GET` · ObservationLog component on PlantingDetailPage
- [x] **Harvest logs** — `POST /api/plantings/{id}/harvests`, `GET` · HarvestLog component on PlantingDetailPage
- [x] **Soil tests** — `POST /api/gardens/{id}/beds/{bedId}/soil-tests`, `GET` · SoilTestLog component on GardenBedDetailPage
- [x] **Amendment logs** — `POST /api/gardens/{id}/beds/{bedId}/amendments`, `GET` · AmendmentLog component on GardenBedDetailPage
- [x] **Pest & disease logs** — `POST /api/gardens/{id}/beds/{bedId}/pest-disease-logs`, `GET`, resolve · PestDiseaseLog component on GardenBedDetailPage
- [x] **Garden members** — `POST /api/gardens/{id}/members` (by email), `DELETE` · MembersSection on GardenDetailPage (owner only add/remove)
- [x] **User settings** — `GET/PUT /api/users/me/settings` · SettingsPage ProfileSection (display name, notification preferences)
- [x] **Weather strip real data** — `GET /api/households/{id}/weather` wired into AppHeader; renamed `WeatherStrip` → `AppHeader` across all pages
- [x] **Plants catalog** — `GET /api/plants/{id}`, companions endpoints · PlantDetailPage with growing info + companion chips (Beneficial/Harmful)
- [x] **Plants — add custom plant** — `POST /api/plants` · CreatePlantPage at `/plants/new`; linked from search no-results state

---

## Phase 4 — Settings & Membership (remaining)

> Enables multi-user households. Most of Phase 4 is done; one backend gap blocks the remainder.

- [ ] **Household management** — `GET/PUT /api/households/{id}`, member management · SettingsPage HouseholdSection UI exists **but `POST /api/households` (CreateHousehold) is not implemented in the backend** — users who have no household cannot create one; they can only update an existing one via the settings page
- [ ] **Fix: householdId null on CreateGardenPage** — `frontend/src/pages/CreateGardenPage.tsx:54` — surface an error or prompt to create/join a household instead of silently doing nothing when `householdId` is null

---

## Phase 5 — Polish & Advanced Features

> Nice-to-have depth. Low urgency until core flows are solid.

- [ ] **Plant companion management UI** — `POST /api/plants/{id}/companions`, `DELETE` · backend endpoints exist (AddPlantCompanion, RemovePlantCompanion) but no frontend UI; PlantDetailPage currently shows companions read-only
- [ ] **User insight creation** — `POST /api/gardens/{id}/insights` · backend endpoint exists (CreateUserInsight) but is not surfaced in the frontend; currently insights are only read and dismissed
- [ ] **Weather station setup** — `GET/PUT/DELETE /api/households/{id}/weather-station` · backend Household slice fully implements weather station CRUD but SettingsPage has no weather station configuration UI

