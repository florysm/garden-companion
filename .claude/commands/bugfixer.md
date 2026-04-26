# Garden Companion — Bug Fixer

Fix one bug from `KnownBugs.md`, run tests, and remove the resolved item.

## Usage

`/bugfixer` — fix the first listed bug  
`/bugfixer [keyword]` — fix the bug whose title or description matches the keyword

## Steps

1. Read `KnownBugs.md`. If the file is empty or has no open items, report "Nothing to fix" and stop.
2. Select the target item: use the keyword argument if provided, otherwise pick the first listed bug.
3. **Classify the item** before writing any code:
   - If the bug requires a new backend endpoint or a significant new feature (e.g., "no handler exists"), do NOT attempt to implement it. Report: "This bug requires a new vertical slice — use `/new-slice` to scaffold it first." Then stop.
   - Otherwise, proceed.
4. Read every source file the bug references. Understand exactly what is wrong before writing anything.
5. Implement the fix. Keep the change minimal — fix the stated defect, do not refactor surrounding code.
6. Run verification:
   - **Backend bug**: `dotnet test` from the repo root. If tests fail, diagnose and fix before continuing.
   - **Frontend bug**: `cd frontend && npm run build` (catches TypeScript errors). If it fails, fix before continuing.
   - **Both**: run both.
7. Update `KnownBugs.md`:
   - Remove the resolved bug entry.
   - Set `Last updated:` to today's date.
8. Report: what the bug was, what changed, which files were modified, test result.

## What NOT to do

- Do not fix multiple bugs in one run — pick one and finish it completely.
- Do not refactor code beyond what is needed to fix the bug.
- Do not mark a bug as fixed without running the verification step.
- Do not attempt bugs classified as "requires new feature" — flag them and stop.
