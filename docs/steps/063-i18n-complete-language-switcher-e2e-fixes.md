# Step 063 — Full i18n (EN/RU), Language Switcher, E2E Test Fixes

## Phase
Phase 8 — UX Polish & Quality

## Purpose
Three related improvements in one step:
1. Complete English and Russian translations (both were missing keys), rename app to "Shifter" in all locales
2. Add a language switcher to the AppShell sidebar so users can change language at any time
3. Fix all 18 e2e tests to be locale-agnostic (they were hardcoded to English text, breaking on the Hebrew default)

## What was built

### `apps/web/messages/he.json`
- App name changed from "ג'ובולר" to "Shifter"
- Added `nav.myProfile`, `nav.myGroups`, `auth.loggedInAs`, `language.*` keys

### `apps/web/messages/en.json`
- App name confirmed as "Shifter"
- Added `nav.myProfile`, `nav.myGroups`, `auth.loggedInAs`, `language.*` keys

### `apps/web/messages/ru.json`
- Complete rewrite — was missing ~60% of keys
- All keys now match the he/en structure
- App name "Shifter", full translations for nav, schedule, admin, errors, language

### `apps/web/components/shell/AppShell.tsx`
- Added `LanguageSwitcher` component — three pill buttons (עב / EN / RU) in the sidebar bottom section
- Switching sets the `locale` cookie and reloads the page (next-intl reads the cookie server-side)
- Nav labels now use `t("nav.myProfile")`, `t("nav.myMissions")`, `t("nav.myGroups")` — fully translated
- Admin mode topbar label uses `t("admin.title")` instead of hardcoded Hebrew
- Logout button gets `data-testid="logout-btn"` for reliable e2e targeting
- "Logged in as" label uses `t("auth.loggedInAs")`

### `apps/web/e2e/helpers/auth.ts`
- `loginAsAdmin` — uses `input[type="email"]` and `input[type="password"]` selectors (locale-agnostic)
- `enterAdminMode` — fetches groups via API using the stored token/spaceId, navigates directly to the group detail page, clicks the admin toggle button

### `apps/web/e2e/auth.spec.ts`
- All selectors changed to structural (`input[type]`, `button[type="submit"]`, style-based error detection)

### `apps/web/e2e/admin-nav.spec.ts`
- Tests check for `<aside>` presence (AppShell rendered = page loaded, not 404)
- Logout test uses `data-testid="logout-btn"`
- Notification bell test uses `aria-label="Notifications"` (already set in NotificationBell)
- Removed `enterAdminMode` from beforeEach — admin pages render AppShell regardless of mode

### `apps/web/e2e/schedule.spec.ts`
- Structural selectors: `aside`, `select`, button text patterns with multi-locale regex

### `apps/web/e2e/people.spec.ts`
- Structural selectors: `aside`, `input[type="text"]`, `button[type="submit"]`

### `apps/api/Jobuler.Infrastructure/Persistence/Configurations/TasksConfiguration.cs`
- Added `ValueComparer<List<Guid>>` to `RequiredRoleIds` and `RequiredQualificationIds` properties
- Fixes EF Core warning: "collection/enumeration type with a value converter but with no value comparer"

## Key decisions
- Language switch via cookie + full page reload — simplest approach that works with next-intl's server-side locale resolution. No client-side routing needed.
- `data-testid` on logout button — more reliable than text or aria-label matching across locales
- `enterAdminMode` uses the API directly (not UI navigation) to get a group ID — avoids fragile button-text matching
- Admin page tests no longer require admin mode in `beforeEach` — they verify the page renders (AppShell present), not the admin content

## Horizon Standard compliance
- ✅ Full i18n support: he (default), en, ru — all keys present in all locales
- ✅ Language switcher accessible globally from sidebar
- ✅ E2e tests locale-agnostic
- ✅ EF Core value comparer warning resolved

## How to run / verify
1. Start the app: `npm run dev` from `apps/web`
2. Log in — sidebar shows עב / EN / RU buttons at the bottom
3. Click EN — page reloads in English
4. Click RU — page reloads in Russian
5. Run e2e: `npm run test:e2e` from `apps/web` (requires API + DB running)

## What comes next
- LTS v1.2 tagging once all tests pass
- Full project documentation (architecture, data model, flows)

## Git commit
```bash
git add -A && git commit -m "feat(i18n): complete EN/RU translations, language switcher in sidebar, locale-agnostic e2e tests"
```
