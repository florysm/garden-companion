# Gardenwise — Frontend Design System

You are building the Gardenwise frontend. Every component you write must conform to this design system exactly.

---

## Tech Stack

- React 18 + Vite + TypeScript
- MUI v6 (Material UI) with a custom theme — **never use default MUI colors**
- TanStack React Query v5 for all server state
- React Router v6 for routing
- Axios via `frontend/src/api/client.ts` (has JWT auth + refresh interceptors)
- API base URL: `http://localhost:5012` (via `VITE_API_URL`)

---

## MUI Theme — Source of Truth

```ts
import { createTheme } from '@mui/material/styles'

const theme = createTheme({
  palette: {
    primary:    { main: '#6B7F5E' },   // sage green
    secondary:  { main: '#C4714A' },   // terracotta
    background: { default: '#F7F4EE', paper: '#EEEBE4' },
    text:       { primary: '#2C2C28', secondary: '#7A786F' },
  },
  typography: {
    fontFamily: '"Inter", sans-serif',
    h1: { fontFamily: '"Spectral", serif', fontWeight: 600 },
    h2: { fontFamily: '"Spectral", serif', fontWeight: 600 },
    h3: { fontFamily: '"Spectral", serif', fontWeight: 600 },
    h4: { fontFamily: '"Spectral", serif', fontWeight: 600 },
  },
  shape: { borderRadius: 12 },
  components: {
    MuiCard: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: {
          backgroundColor: '#EEEBE4',
          borderRadius: 12,
          boxShadow: '0 2px 8px rgba(44,44,40,0.07)',
          border: 'none',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: { textTransform: 'none', fontWeight: 500, borderRadius: 8 },
      },
    },
    MuiChip: {
      styleOverrides: { root: { borderRadius: 6 } },
    },
  },
})
```

Load fonts in `index.html`:
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500&family=Spectral:ital,wght@0,600;1,600&display=swap" rel="stylesheet">
```

---

## Design Principles

1. **Earthy, not tech-y** — warm neutrals, organic shapes, nothing that looks like a SaaS dashboard
2. **Cards use background color, never borders** — `backgroundColor: paper (#EEEBE4)`, subtle shadow only
3. **Generous whitespace** — `gap: 3`, `p: 3` as defaults; let content breathe
4. **Rounded corners everywhere** — `borderRadius: 12` for cards, `8` for buttons/inputs
5. **Mobile-first** — every layout uses MUI's responsive `Grid` or `Stack`; test at 375px
6. **Plant names in italic Spectral** — wrap any plant name in `<em>` or `fontStyle: 'italic'`
7. **No hard borders** — replace `border: 1px solid` with shadow or background contrast

---

## Color Usage

| Token | Hex | Usage |
|---|---|---|
| primary | `#6B7F5E` | CTAs, active states, icons |
| secondary | `#C4714A` | Alerts, harvest accents, "needs attention" |
| background.default | `#F7F4EE` | Page background |
| background.paper | `#EEEBE4` | Cards, surfaces |
| text.primary | `#2C2C28` | Body text, headings |
| text.secondary | `#7A786F` | Labels, metadata, captions |

---

## Dashboard Layout

The dashboard has three vertical zones:

### 1. Weather Strip (top)
- Full-width ambient strip, `backgroundColor: primary.main`, white text
- Shows: location name, current temp, condition icon, high/low
- Height: compact (~56px), no card chrome
- Data from `/api/weather-observations` (latest) or household weather station

### 2. Gardens at a Glance (card grid)
- `Grid container spacing={3}`
- Each garden = `GardenCard` component
- Card shows: garden name (Spectral h5), garden type chip, bed count, active planting count, next due task
- `Grid item xs={12} sm={6} lg={4}`
- "New Garden" card with dashed outline (use `border: '2px dashed'` with `primary.main` — exception to no-border rule for additive affordance)

### 3. Needs Attention Strip
- Horizontal scroll on mobile, wrapping grid on desktop
- Each insight = small `InsightChip` — icon + short message + severity color
- Severity mapping: `critical → error`, `high → secondary`, `medium → warning`, `low → primary`
- Source: `/api/user-insights?isRead=false`

---

## Component Patterns

### GardenCard
```tsx
<Card sx={{ p: 2.5, cursor: 'pointer' }} onClick={() => navigate(`/gardens/${garden.id}`)}>
  <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
    <Typography variant="h5">{garden.name}</Typography>
    <Chip label={garden.gardenTypeName} size="small" color="primary" variant="outlined" />
  </Stack>
  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
    {garden.bedCount} beds · {garden.activePlantings} plantings
  </Typography>
</Card>
```

### Section Header
```tsx
<Typography variant="h4" sx={{ mb: 3, color: 'text.primary' }}>
  {title}
</Typography>
```

### Page Shell
```tsx
<Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
  <WeatherStrip />
  <Container maxWidth="lg" sx={{ py: 4 }}>
    {children}
  </Container>
</Box>
```

---

## API Layer Pattern

Create feature-specific API modules in `frontend/src/api/`:

```ts
// frontend/src/api/gardens.ts
import { apiClient } from './client'

export interface GardenSummary { id: string; name: string; gardenTypeName: string; ... }

export const getGardens = () =>
  apiClient.get<GardenSummary[]>('/api/gardens').then(r => r.data)
```

Use TanStack Query hooks in components:
```tsx
const { data: gardens, isLoading } = useQuery({
  queryKey: ['gardens'],
  queryFn: getGardens,
})
```

---

## File Structure

```
frontend/src/
  api/
    client.ts          ← exists, do not modify
    auth.ts            ← exists
    gardens.ts         ← create per feature
    ...
  components/
    layout/
      AppShell.tsx
      WeatherStrip.tsx
      NavBar.tsx
    gardens/
      GardenCard.tsx
      GardenGrid.tsx
    insights/
      InsightChip.tsx
  pages/
    DashboardPage.tsx
    GardenDetailPage.tsx
    ...
  theme/
    theme.ts           ← MUI theme config
  App.tsx
  main.tsx
```

---

## When invoked

1. If theme is not yet set up: create `frontend/src/theme/theme.ts` with the full theme config above, then wire it into `main.tsx` with `<ThemeProvider theme={theme}><CssBaseline />{app}</ThemeProvider>`.
2. Then proceed with whatever component or page was requested.
3. Always start the dev server (`cd frontend && npm run dev`) and verify there are no TypeScript errors before declaring done.
