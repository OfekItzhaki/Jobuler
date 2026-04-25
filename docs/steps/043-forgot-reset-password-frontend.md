# Step 043 — Forgot / Reset Password Frontend

## Phase
Phase 8 — Auth UX

## Purpose
Expose the password-reset flow to users via two new pages (`/forgot-password` and `/reset-password`) and wire them into the existing login page, completing the end-to-end forgot-password experience that the backend already supports (steps 039–042).

## What was built

| File | Change |
|------|--------|
| `apps/web/lib/api/auth.ts` | Added `forgotPassword(email)` and `resetPassword(token, newPassword)` — thin wrappers around the existing `apiClient`. `forgotPassword` silently swallows errors to avoid email enumeration. |
| `apps/web/app/forgot-password/page.tsx` | New page. Renders an email form; on submit calls `forgotPassword` and shows a generic success message regardless of outcome. |
| `apps/web/app/reset-password/page.tsx` | New page. Reads `?token=` from the URL, validates password length and match client-side, calls `resetPassword`, then redirects to `/login?reset=1` on success. Uses `<Suspense>` to wrap `useSearchParams`. |
| `apps/web/app/login/page.tsx` | Added "שכחת סיסמה?" link below the password field and a green success banner when `?reset=1` is present in the URL. |

## Key decisions

- `forgotPassword` never throws — the UI always shows the same "check your inbox" message to prevent email enumeration attacks.
- `resetPassword` propagates errors so the UI can display "Invalid or expired token" when the backend rejects the token.
- `useSearchParams` is wrapped in `<Suspense>` on the reset page to satisfy Next.js App Router requirements.
- Error type is cast via an intermediate interface rather than `any` to satisfy strict TypeScript.

## How it connects

- Calls `POST /auth/forgot-password` and `POST /auth/reset-password` implemented in step 042.
- On success, redirects to `/login?reset=1` which triggers the new banner added to the login page.
- The "שכחת סיסמה?" link on the login page leads to `/forgot-password`.

## How to run / verify

1. Start the API and web app.
2. Navigate to `/login` — confirm "שכחת סיסמה?" link appears below the password field.
3. Click the link → `/forgot-password` — submit any email, confirm the success message appears.
4. Use a valid reset token (from email or DB) and navigate to `/reset-password?token=<token>` — submit a new password, confirm redirect to `/login?reset=1` with the success banner.

## What comes next

- Email delivery integration (SMTP / SendGrid) so reset tokens actually reach users.
- Token expiry UX — prompt user to re-request if the token is expired.

## Git commit

```bash
git add -A && git commit -m "feat(auth): forgot/reset password frontend pages and login page links"
```
