# Garden Companion — Bug Tracker

Scan the codebase for bugs and update `KnownBugs.md`.

## Usage

`/bugtracker` — scan both backend and frontend  
`/bugtracker frontend` — frontend only  
`/bugtracker backend` — backend only

## Steps

1. Read `KnownBugs.md` to see what is already tracked.
2. Spawn parallel Explore subagents (one backend, one frontend) — give each the search targets from the relevant section below. Skip the section that does not match the scope argument.
3. For every candidate finding, read the relevant file to verify the bug exists before recording it.
4. Update `KnownBugs.md`:
   - Add newly confirmed bugs not already tracked.
   - Remove bugs that are no longer present in the code.
   - Set `Last updated:` to today's date.
5. Report a brief summary: N new bugs found, N resolved and removed.

## What counts as a bug

A concrete, verifiable defect — wrong behavior, data corruption, silent failure, or security gap. Not a style issue or a missing feature. Only report findings you have confirmed by reading the source file.

## Backend search targets

- `Features/Households/` — confirm `POST /api/households` (CreateHousehold) has no handler; check if any other registered routes are missing their handler file
- Endpoints missing `.RequireAuthorization()` in their `Map()` method — check `Program.cs` registrations
- Handlers that access `.Include()`-dependent navigation properties without the corresponding `.Include()` in the query
- `catch` blocks that swallow exceptions silently (no return value, no log)
- Validation gaps: request fields the validator does not check but the handler uses
- EF Core soft-delete: any query on `Plantings` that adds a manual `WHERE DeletedAt IS NULL` (redundant with the global query filter and can signal misunderstanding)

## Frontend search targets

- `useEffect` with missing dependency array entries (stale closures)
- `localStorage` values read on mount but not cleared on logout (`AuthContext.tsx`, token handling)
- API calls gated on `if (householdId)` or `if (userId)` that silently do nothing on null
- Mutation `onError` callbacks that are missing or empty (silent failures)
- Date construction: `new Date(dateString)` where `dateString` is a `YYYY-MM-DD` value — shifts date for users west of UTC
- `if (value)` / `if (updates.field)` guards that block valid falsy values (`0`, `false`, `null`)
- Form state seeded from async data that goes stale on query refetch

## Entry format

New entries must follow this format:

```
**Short description**

What goes wrong, why, and under what condition.

- File: `path/to/file.tsx:lineNumber`
- Impact: [user-visible / data loss / auth / silent failure]
```
