# Step 104 — Shifter Logo Rebrand

## Phase
Phase 9 — Polish & Hardening

## Purpose
The app icon was using a blue background with a rounded "S" curve. The new design uses a white background with a sharper, less-round "S" (square stroke caps, miter joins).

## What was built

### `apps/web/components/shell/ShifterLogo.tsx` (new)
Shared logo component. Accepts a `size` prop (default 32 for sidebar, 40 for auth pages).

Design:
- White background with subtle drop shadow
- Corner radius = 25% of size (e.g. 8px at size=32, 10px at size=40)
- Blue `#3b82f6` "S" path with `strokeLinecap="square"` and `strokeLinejoin="miter"` for sharper angles

### Updated files
All five places that previously rendered the inline logo SVG now import and use `<ShifterLogo />`:

- `apps/web/components/shell/AppShell.tsx` — sidebar logo (size=32)
- `apps/web/app/login/page.tsx` — auth page header (size=40)
- `apps/web/app/register/page.tsx` — auth page header (size=40)
- `apps/web/app/forgot-password/page.tsx` — auth page header (size=40)
- `apps/web/app/reset-password/page.tsx` — auth page header (size=40)

## Key decisions
- Single source of truth: one component, no more duplicated inline SVG across 5 files.
- `strokeLinecap="square"` + `strokeLinejoin="miter"` gives the sharper, less-round look.
- White background works on both the dark sidebar and the light auth pages.

## How to verify
Open the app — the sidebar logo and all auth page logos should show a white rounded-square icon with a blue "S".

## Git commit

```bash
git add -A && git commit -m "feat(ui): rebrand Shifter logo — white background, sharper S, shared component"
```
