# Frontend Roadmap

Last updated: 2026-04-23. Prioritized by user impact — higher phases assume lower phases are mostly done.

---

## Completed

- [x] Auth flow — login, register, forgot/reset password, token refresh interceptor
- [x] Dashboard — garden grid, skeletons, empty state, Insights "Needs Attention" section
- [x] Gardens — create, detail (beds grid)
- [x] Garden Beds — create, detail (plantings grid with status chips)
- [x] Plantings — create (plant search, auto harvest date), detail (status progression, stat cards)
- [x] Garden Tasks — create, list (Open/Completed), complete (optimistic), delete (optimistic)

---

## Phase 1 — Core Usability (edit & delete)

> Users hit these on day one. Can't fix a typo, can't remove a mistake.

- [ ] **Edit garden** — `PUT /api/gardens/{id}` · edit page or inline form on GardenDetailPage (name, description, types)
- [ ] **Delete garden** — `DELETE /api/gardens/{id}` · confirmation dialog on GardenDetailPage (owner only)
- [ ] **Edit garden bed** — `PUT /api/gardens/{id}/beds/{bedId}` · edit page from GardenBedDetailPage (name, type, shape, sun, dimensions)
- [ ] **Delete garden bed** — `DELETE /api/gardens/{id}/beds/{bedId}` · confirmation dialog; backend returns 409 if active plantings exist — surface that clearly
- [ ] **Edit planting** — `PUT /api/plantings/{id}` (check route) · edit page from PlantingDetailPage (dates, quantity, type)
- [ ] **Delete planting** — `DELETE /api/plantings/{id}` · confirmation dialog on PlantingDetailPage
- [ ] **Edit task** — `PUT /api/gardens/{id}/tasks/{taskId}` · inline edit or edit page from task row (title, type, due date, bed)

---

## Phase 2 — Planting Log Completions

> PlantingDetailPage already shows observation and harvest counts. These close the dead ends.

- [x] **Planting observations** — `POST /api/plantings/{id}/observations`, `GET /api/plantings/{id}/observations` · add observation form + observation list on PlantingDetailPage (notes, observed date, optional photo URL)
- [x] **Harvest logs** — `POST /api/plantings/{id}/harvests`, `GET /api/plantings/{id}/harvests` · log harvest form + harvest list on PlantingDetailPage (quantity, unit, harvest date, notes)

---

## Phase 3 — Bed-Level Logging

> Deepens GardenBedDetailPage from a plantings list into a full bed record.

- [x] **Soil tests** — `POST /api/gardens/{id}/beds/{bedId}/soil-tests`, `GET` · soil test list + add form on GardenBedDetailPage (pH, N/P/K, test date, notes)
- [x] **Amendment logs** — `POST /api/gardens/{id}/beds/{bedId}/amendments`, `GET` · amendment log on GardenBedDetailPage (amendment type, quantity, date)
- [x] **Pest & disease logs** — `POST /api/gardens/{id}/beds/{bedId}/pest-disease-logs`, `GET`, resolve · log + resolve flow on GardenBedDetailPage (pest/disease name, severity, treatment, resolved date)

---

## Phase 4 — Settings & Membership

> Enables multi-user households and per-user preferences. Likely needed once a second household member tries to use the app.

- [ ] **Garden members** — `POST /api/gardens/{id}/members` (by email), `DELETE` · member list + invite/remove UI on GardenDetailPage (owner only for add/remove)
- [ ] **Household management** — `GET/PUT /api/households/{id}`, member management · settings page (household name, member list, invite by email)
- [ ] **User settings** — `GET/PUT /api/users/me/settings` · settings page section (display name, notification preferences)
- [ ] **Fix: householdId null on CreateGarden** — `frontend/src/pages/CreateGardenPage.tsx:54` — surface an error or prompt to create/join a household instead of silently doing nothing

---

## Phase 5 — Advanced Features

> Nice-to-have depth. Low urgency until core flows are solid.

- [ ] **Weather strip real data** — wire `GET /api/weather-observations` into WeatherStrip; show current conditions alongside the greeting. Rename component to `AppHeader` until implemented.
- [ ] **Plants catalog** — `GET /api/plants/{id}`, companions endpoints · plant detail page accessible from search results; companion planting relationships
- [ ] **Plants — add custom plant** — `POST /api/plants` · form to add a plant not in the global catalog (name, family, days to maturity)

---

## Bugs

- [ ] **GardenCard only renders first garden type** — `frontend/src/components/gardens/GardenCard.tsx:18` · show all type chips or first + `+N` overflow indicator

---

## Tech Debt

Opportunistic — fix alongside whichever phase touches the relevant file.

- [ ] **Status chip styles duplicated** — `GardenBedDetailPage.tsx:20` and `PlantingDetailPage.tsx:19` have near-identical status→style mappings. Extract to `src/utils/plantingStatus.ts`.
- [ ] **`useDebounce` is inline** — `CreatePlantingPage.tsx:40`. Move to `src/hooks/useDebounce.ts` before any second search field is added.
- [ ] **`WeatherStrip` misnamed** — rename to `AppHeader` to match what it actually does until real weather data is wired up (Phase 5).
