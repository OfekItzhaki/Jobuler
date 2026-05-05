# Step 099 — Admin Pages i18n

## Phase
Phase 9 — Polish & Hardening

## Purpose
Three admin pages had hardcoded strings that bypassed the i18n system, meaning they would always display in the wrong language for Hebrew and Russian users. This step brings them fully in line with the rest of the app.

## What was built

### Translation files — new keys added to all three locales

**`apps/web/messages/en.json`** / **`he.json`** / **`ru.json`**

Added three new nested sections under `"admin"`:

- `admin.logs` — title, subtitle, column headers (level, source, message, time), empty state, loading
- `admin.stats` — page title, subtitle, "Published versions" label, error message
- `admin.schedule` — all action feedback messages: publish success/error, rollback success/error, override applied/error, shift cleared/error, shift not found

### `apps/web/app/admin/logs/page.tsx`

Previously had zero i18n — every visible string was hardcoded Hebrew. Replaced with `useTranslations("admin")` and `useTranslations("admin.logs")`. Also removed the hardcoded `"he-IL"` locale from the date formatter so it respects the user's browser locale.

### `apps/web/app/admin/stats/page.tsx`

Used `useTranslations` for some strings but had hardcoded English for the page title ("Statistics"), subtitle ("Burden and fairness data by person"), the "Published versions" summary card label, and the error message. All replaced with `tStats(...)` calls via `useTranslations("admin.stats")`.

### `apps/web/app/admin/schedule/page.tsx`

Used `useTranslations("admin")` for most UI but had hardcoded English in all action handler feedback messages (publish, rollback, override, clear). Added `useTranslations("admin.schedule")` and replaced all hardcoded strings.

## Key decisions

- Used nested translation keys (`admin.logs`, `admin.stats`, `admin.schedule`) rather than flat keys to keep the `admin` namespace from growing unwieldy.
- The `admin.schedule` sub-namespace covers only the dynamic feedback messages (toasts/banners) — static UI labels like button text were already i18n'd.
- `rollbackSuccess` uses an interpolated `{newVersionId}` parameter, consistent with how `solverStarted` uses `{runId}`.

## How it connects

- These pages are only accessible in admin mode, so the impact is limited to admins — but admins can also be Hebrew or Russian speakers.
- The translation structure mirrors the existing pattern used by `groups.stats_tab`, `groups.schedule_tab`, etc.
- No backend changes required — this is purely frontend string extraction.

## How to run / verify

1. Switch the app language to Hebrew or Russian via the language switcher.
2. Navigate to Admin → Logs, Admin → Statistics, Admin → Schedule.
3. All visible text should now render in the selected language.
4. Publish a version, trigger a rollback, apply an override — the feedback banners should appear in the active language.

## What comes next

- LTS v1.5 tag — the codebase is now clean enough for a checkpoint release.
- Twilio approved WhatsApp templates (account-level setup, not a code task).

## Git commit

```bash
git add -A && git commit -m "feat(i18n): admin pages full i18n — logs, stats, schedule"
```
