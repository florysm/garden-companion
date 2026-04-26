# Gardenwise — Code Cleaner

Scan the codebase for tech debt and refactoring opportunities, then update `TechDebt.md`.

## Usage

`/codecleaner` — scan both backend and frontend  
`/codecleaner frontend` — frontend only  
`/codecleaner backend` — backend only

## Steps

1. Read `TechDebt.md` in full to see what is already tracked.
2. Spawn parallel Explore subagents (one backend, one frontend) — give each the search targets from the relevant section below. Skip the section that does not match the scope argument.
3. For every candidate, read the relevant files to confirm the issue and count exactly how many locations are affected.
4. Update `TechDebt.md`:
   - Add new items under the appropriate priority tier (High / Medium / Lower Priority).
   - Remove items that are no longer present in the code.
   - Update scope counts in existing items if they have changed (e.g., "9 files" → "12 files").
   - Set `Last updated:` to today's date.
5. Report a brief summary: N new items, N resolved and removed, N scope counts updated.

## What counts as tech debt

Concrete duplication, inconsistency, or missing abstraction — verifiable by reading the code. Minimum bar to record: the same problem appears in 2+ files, or a pattern is broken in a way that will cause real pain when extending the codebase. Do not record style preferences.

## Priority guidance

- **High**: will cause a bug or significant maintenance cost before the next feature is complete
- **Medium**: pain grows linearly as the codebase grows — address within a few sprints
- **Lower Priority**: annoying but stable — address opportunistically

## Backend search targets

- `catch (KeyNotFoundException)` blocks — are all returning `Results.NotFound(new { error = ex.Message })`? Note any that return bare `Results.NotFound()`
- LINQ `Select` projection duplicated across 2+ handlers in the same feature folder
- List endpoints without a `limit`/pagination parameter: `GetPlantings`, `GetGardenTasks`, `GetPestDiseaseLogs`, `GetUserInsights`, `GetAmendmentLogs`
- DTOs or records defined inline inside a handler file rather than in `{Feature}Dtos.cs`
- Manual enum parsing (`Enum.TryParse`) outside the endpoint layer (should be in the endpoint, not the handler)
- `Results.Problem(ex.Message, statusCode: 403)` vs `Results.Forbid()` — find any inconsistencies
- Authorization pattern: count how many handlers call `GardenAccess.Require*Async` at the top — note if any skip it unexpectedly

## Frontend search targets

- `formatDate`, `todayIso`, `today` — grep for definitions; count how many files define their own copy
- Inline component definitions duplicated across files (chip-select, status chip maps, log toggle sections)
- `sx` style objects copy-pasted across files (bgcolor/borderColor/hover patterns)
- Constant arrays (task types, planting types, source types, season types) defined in both a create page and its matching edit page
- Edit pages using `initialized` flag pattern — count how many still use it vs `useEffect + reset()`
- Missing global infrastructure: `<ErrorBoundary>` in `App.tsx`, `NotFoundPage` on catch-all route, toast/snackbar provider
- Mutation `onError` callbacks that are missing — these are both a bug risk and a debt item if widespread

## Entry format

Match the existing format in `TechDebt.md` — bold numbered title, description paragraph, affected file list with line numbers where relevant.
