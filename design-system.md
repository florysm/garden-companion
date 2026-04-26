# Gardenwise App — Design System

## Product Feel
A quality gardening journal — not a SaaS dashboard or tech product.
Three words: calm, grounded, confident.

North star test: "Would an enthusiastic home gardener actually use this on a Tuesday evening after work?"

---

## Color Palette

| Role | Name | Hex |
|---|---|---|
| Primary | Sage green | `#6B7F5E` |
| Accent | Terracotta | `#C4714A` |
| Background | Linen off-white | `#F7F4EE` |
| Surface | Warm light gray | `#EEEBE4` |
| Text primary | Deep charcoal | `#2C2C28` |
| Text secondary | Muted warm gray | `#7A786F` |

Never use pure white (#FFFFFF) or pure black (#000000).
Background is always linen — never stark white.

---

## Typography

### Fonts
- **Spectral** — Google Fonts, free
- **Inter** — Google Fonts, free

### Usage Rules
| Use | Font | Weight | Style |
|---|---|---|---|
| Screen titles, garden names | Spectral | 600 | Normal |
| Plant scientific names | Spectral | 400 | Italic |
| Observation notes | Spectral | 400 | Italic |
| UI labels, buttons, nav | Inter | 500 | Normal |
| Body text, descriptions | Inter | 400 | Normal |
| Secondary labels, metadata | Inter | 400 | Normal |

The Spectral italic on plant scientific names (e.g. *Solanum lycopersicum*)
is a signature detail of this app — use it consistently.

---

## Component Principles

### Cards
- Background color differences separate sections — no hard borders
- Soft rounded corners — `border-radius: 12px`
- Warm subtle shadow — never sharp or deep
- Comfortable internal padding — `24px`
- Surface color: `#EEEBE4`

### Spacing
- Generous whitespace — let content breathe
- Minimum `16px` between card elements
- Section spacing `32px` minimum
- Never crowd elements

### Buttons
- Primary actions: terracotta `#C4714A` background, white text
- Secondary actions: sage green `#6B7F5E` outline
- Destructive actions: muted red, never aggressive
- Rounded: `border-radius: 8px`

### Forms
- Clean input fields with warm gray borders
- Labels above inputs, never placeholder-only
- Validation errors in warm red — not harsh
- Comfortable field height

---

## Dashboard Layout

```
┌─────────────────────────────────────────────┐
│  Good morning, [Name]       🌤 72°F · 0.2"  │  ← ambient weather strip
├─────────────────────────────────────────────┤
│  Your Gardens                               │
│  ┌──────────────┐  ┌──────────────┐         │
│  │ Vegetable    │  │ Berry Patch  │         │  ← garden cards
│  │ 4 beds       │  │ 2 beds       │         │
│  │ 12 plantings │  │ 3 plantings  │         │
│  └──────────────┘  └──────────────┘         │
├─────────────────────────────────────────────┤
│  Needs Attention                            │
│  · Tomatoes in Bed 2 — ready to harvest     │  ← insight strip
│  · No rain in 6 days                        │
└─────────────────────────────────────────────┘
```

Weather is ambient and supporting — not the hero of the screen.
Gardens and beds at a glance is the primary dashboard purpose.

---

## Screen Priority

### Design carefully — high interaction:
- Dashboard
- Garden bed detail — bed layout, planting cards, quick actions
- Add planting flow — plant search, companion warnings, depth validation
- Planting detail — lifecycle status, observation log, harvest history

### Generate directly — simpler screens:
- Auth screens — login, register, forgot/reset password
- Settings — form based
- List screens — gardens list, beds list

---

## Mobile
- Mobile friendly from day one
- Touch targets minimum `44px`
- Bottom navigation on mobile
- Cards stack to full width on small screens
- Weather strip collapses gracefully

---

## What to Avoid
- Pure white backgrounds
- Hard borders between cards
- Bright or saturated colors
- Generic MUI default styling
- Dense data tables without breathing room
- Anything that looks like a tech dashboard
- Gradients
- Drop shadows that are too heavy or sharp
