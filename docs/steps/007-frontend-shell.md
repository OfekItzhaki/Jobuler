# Step 007 — Next.js Frontend Shell

## Phase
Phase 1 — Foundation

## Purpose
Bootstrap the Next.js frontend with auth flow, viewer/admin mode shell, Hebrew RTL as default, and full i18n scaffold for Hebrew, English, and Russian. No schedule data is rendered yet — this step establishes the structural foundation.

## What was built

| File | Description |
|---|---|
| `apps/web/package.json` | Next.js 14, next-intl, zustand, axios, react-query, tailwind |
| `apps/web/next.config.ts` | next-intl plugin, standalone output for Docker |
| `apps/web/tsconfig.json` | Strict TypeScript config with `@/*` path alias |
| `apps/web/tailwind.config.ts` | Tailwind configured for app/, components/, lib/ |
| `apps/web/i18n/request.ts` | next-intl server config; reads locale from cookie; RTL detection |
| `apps/web/messages/he.json` | Hebrew translations (primary language) |
| `apps/web/messages/en.json` | English translations |
| `apps/web/messages/ru.json` | Russian translations |
| `apps/web/lib/api/client.ts` | Axios client with JWT interceptor and auto-refresh on 401 |
| `apps/web/lib/api/auth.ts` | `login()`, `register()`, `logout()` API calls |
| `apps/web/lib/store/authStore.ts` | Zustand store: auth state, admin mode toggle, login/logout |
| `apps/web/app/layout.tsx` | Root layout: sets `lang` and `dir` attributes from locale |
| `apps/web/app/globals.css` | Tailwind base + RTL text-align rule |
| `apps/web/app/page.tsx` | Root redirect to `/schedule/today` |
| `apps/web/app/login/page.tsx` | Login form with i18n labels and error handling |
| `apps/web/app/schedule/today/page.tsx` | Today's schedule page (shell, data in Phase 5) |
| `apps/web/app/schedule/tomorrow/page.tsx` | Tomorrow's schedule page (shell, data in Phase 5) |
| `apps/web/components/shell/AppShell.tsx` | Top nav with viewer/admin mode toggle, logout |
| `apps/web/middleware.ts` | Route guard: redirects unauthenticated users to `/login` |

## Key decisions

### Hebrew RTL from day one
`app/layout.tsx` sets `dir="rtl"` when locale is `he`. Tailwind's utility classes work correctly with RTL because they use logical properties where possible. This is wired at the root layout level so it applies to every page.

### Locale from cookie
`next-intl` reads the locale from a `locale` cookie set at login time. This means the locale follows the user's account preference (stored in the DB) and persists across sessions without URL-based locale routing.

### Admin mode is a UI state, not a security boundary
`isAdminMode` in the Zustand store controls what UI elements are visible. The actual security enforcement is on the API — every admin action requires a valid permission check server-side. The frontend toggle is UX only.

### Auto-refresh on 401
The Axios interceptor catches 401 responses, attempts a token refresh, and retries the original request once. If the refresh fails, it clears tokens and redirects to `/login`. This keeps the user session alive transparently.

### Middleware route guard
`middleware.ts` checks for an `access_token` cookie. This is a lightweight redirect guard — it does not validate the JWT signature (that happens on the API). It prevents unauthenticated users from seeing any page content.

## How it connects
- Calls `POST /auth/login` and `POST /auth/refresh` from Step 004.
- Calls `GET /spaces` from Step 005 (wired in Phase 5 space selector).
- `AppShell` admin mode toggle gates access to admin routes added in Phase 5.

## How to run / verify

```bash
cd apps/web
npm install
npm run dev
# Open http://localhost:3000
# Should redirect to /login
# Login with admin@demo.local / Demo1234! (after seed data is loaded)
# Should redirect to /schedule/today with Hebrew RTL layout
```

## What comes next
- Phase 2: People, tasks, groups management pages
- Phase 5: Schedule tables with real data, search, diff view, publish/rollback UI
