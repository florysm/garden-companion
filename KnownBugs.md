# Known Bugs

Last updated: 2026-04-25.

---

**ObservationLog date stored as UTC midnight instead of local date**

When adding an observation, `new Date(observedAt).toISOString()` (line 89) parses the YYYY-MM-DD date input as UTC midnight. For users west of UTC, the stored timestamp falls on the prior calendar day, so the observation displays one day earlier than entered. The fix is to append `'T00:00:00'` before constructing the Date (as PestDiseaseLog correctly does at line 183).

- File: `frontend/src/components/plantings/ObservationLog.tsx:89`
- Impact: user-visible

---

**PestDiseaseLog "Mark resolved" fails silently**

The `resolveMutation` inside `LogRow` (lines 83–88) has no `onError` handler and no `resolveMutation.isError` display in the JSX. If the API call fails, the button re-enables with no feedback — the user has no way to know the action failed.

- File: `frontend/src/components/beds/PestDiseaseLog.tsx:83`
- Impact: silent failure

---

**SettingsPage household member removal fails silently**

The `removeMutation` (lines 276–279) has no `onError` handler and no error UI in the member list. If removing a household member fails, the UI shows nothing — the delete icon just re-enables.

- File: `frontend/src/pages/SettingsPage.tsx:276`
- Impact: silent failure

---

**GardenDetailPage garden member removal fails silently**

The `removeMutation` in `MembersSection` (lines 360–363) has no `onError` handler and no `removeMutation.isError` display. If removing a garden member fails, the UI shows nothing.

- File: `frontend/src/pages/GardenDetailPage.tsx:360`
- Impact: silent failure

---

**AuthContext falsy guard skips localStorage update for empty displayName**

`if (updates.displayName)` at line 77 uses truthiness, so passing `displayName: ''` skips `localStorage.setItem`. React state updates but localStorage keeps the old value, causing a desync that persists across page reloads. The guard should be `if ('displayName' in updates)`.

- File: `frontend/src/contexts/AuthContext.tsx:77`
- Impact: silent failure (localStorage/state desync on empty display name)
