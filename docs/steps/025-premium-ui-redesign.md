# Step 025 — Premium UI Redesign

## Phase
Phase 8 — Polish & UX

## Purpose
The app was functional but visually plain. This step replaces the horizontal top-nav layout with a premium dark sidebar design, upgrades all pages to a consistent card-based visual language, and improves typography, spacing, and interactive states throughout.

## What was built

### Modified files

| File | Change |
|------|--------|
| `apps/web/app/globals.css` | Added Inter font import via Google Fonts, custom scrollbar styles, CSS variables for sidebar and accent colors |
| `apps/web/components/shell/AppShell.tsx` | Full redesign: fixed dark sidebar (slate-900, w-64) with inline SVG icons, sticky top bar with admin mode indicator, mobile drawer with overlay, RTL-aware layout using `ms-`/`me-` |
| `apps/web/app/login/page.tsx` | Premium centered card with logo mark, background blur decorations, animated spinner, improved error state |
| `apps/web/app/spaces/page.tsx` | Matching card design with workspace list, avatar icons, chevron indicators |
| `apps/web/components/schedule/ScheduleTable.tsx` | Upgraded table with proper header styling, colored source badges (Override = amber, others = slate), empty state illustration |
| `apps/web/app/schedule/today/page.tsx` | Page header with live indicator badge, loading spinner, improved empty state |
| `apps/web/app/schedule/tomorrow/page.tsx` | Same treatment as today page |
| `apps/web/app/admin/schedule/page.tsx` | StatusBadge component, improved version list cards, action button bar with icons, download buttons |
| `apps/web/app/admin/people/page.tsx` | Active/inactive status badges with dot indicators, improved create form, count in subtitle |
| `apps/web/app/admin/tasks/page.tsx` | Burden level badges with color-coded dots, improved tab styling, consistent form layout |
| `apps/web/app/admin/constraints/page.tsx` | Severity badges (hard=red, soft=blue), success/error banners with icons |
| `apps/web/app/admin/groups/page.tsx` | Member panel as card, avatar initials for members, selected group highlight in table |

## Key decisions

- **Dark sidebar (slate-900)** — fixed on desktop, drawer on mobile with backdrop overlay
- **No icon library** — all icons are inline SVGs to avoid adding a dependency
- **RTL-aware** — all directional spacing uses `ms-`/`me-` instead of `ml-`/`mr-`; sidebar uses `start-0`
- **Tailwind only** — no new CSS classes, no CSS modules
- **All business logic preserved** — only JSX structure and Tailwind classes changed
- **Status badges** — consistent pill badges with colored dot indicators across all tables
- **Admin mode** — amber color scheme in top bar + sidebar section label when active

## How it connects

- `AppShell` wraps every authenticated page — this change affects all screens simultaneously
- `ScheduleTable` is used by both schedule pages and the admin schedule page
- `globals.css` loads Inter font for the entire app

## How to run / verify

```bash
cd apps/web
npm run dev
```

1. Visit `/login` — should show centered card with logo
2. Visit `/spaces` — should show workspace list with icons
3. Navigate to `/schedule/today` — dark sidebar visible, schedule table with badges
4. Toggle admin mode — top bar turns amber, admin nav section appears in sidebar
5. Visit each admin page — consistent table styling, form cards, status badges

## Git commit

```bash
git add -A && git commit -m "feat(ui): premium dark sidebar redesign across all screens"
git push
```
