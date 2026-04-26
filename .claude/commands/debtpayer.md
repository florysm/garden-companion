# Gardenwise — Debt Payer

Address one tech debt item from `TechDebt.md`, run checks, and update the file.

## Usage

`/debtpayer` — address the first High-priority item  
`/debtpayer [keyword]` — address the item whose title matches the keyword

## Steps

1. Read `TechDebt.md` in full. If no open items exist, report "Nothing to address" and stop.
2. Select the target item: use the keyword argument if provided, otherwise pick the first item under **High Priority**.
3. Read every source file the item references to understand the full scope of the change.
4. **Classify the item** (see criteria below) as **mechanical** or **structural**.
5. Follow the path for that classification (see below).
6. Run verification after all changes are made:
   - **Backend changes**: `dotnet test` from the repo root.
   - **Frontend changes**: `cd frontend && npm run build` (TypeScript check). If the project has a test suite, run it too.
   - Fix any failures before proceeding.
7. Update `TechDebt.md`:
   - Remove the resolved item.
   - Update scope counts on any related items whose affected-file counts changed.
   - Set `Last updated:` to today's date.
8. Report: what the item was, what changed, which files were modified, verification result.

---

## Mechanical items — implement directly

A change is **mechanical** if it meets all of these:
- The transformation is deterministic (extract X from N files and put it in one place)
- No behavioral change — inputs/outputs stay identical, only the location of code moves
- The affected files are already known and listed in TechDebt.md

**Examples**: extracting a duplicated utility function, moving constants to a shared file, renaming a component, extracting an inline component that is copy-pasted.

**Process**: Read all affected files → make the changes → run verification → update TechDebt.md.

---

## Structural items — plan first, then implement

A change is **structural** if it involves:
- Changing a pattern or convention across multiple pages/components (e.g., replacing the `initialized` flag pattern with `useEffect + reset()`)
- Introducing new shared infrastructure (error boundary, toast provider, 404 page)
- Changing how state flows or how side effects are triggered

**Process**:
1. Read all affected files.
2. Write out a concrete plan: what changes in each file, in what order, and why.
3. **Stop and ask the user to confirm before writing any code.**
4. After confirmation, implement the changes file by file.
5. Run verification.
6. Update TechDebt.md.

---

## What NOT to do

- Do not address multiple debt items in one run — one item, fully completed.
- Do not refactor beyond the scope of the selected item.
- Do not remove an item from TechDebt.md without running the verification step first.
- Do not treat a structural item as mechanical to skip the confirmation step.
