# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: people.spec.ts >> People management >> people list page loads
- Location: e2e\people.spec.ts:11:7

# Error details

```
TimeoutError: page.waitForFunction: Timeout 15000ms exceeded.
```

# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - main [ref=e2]:
    - generic [ref=e3]:
      - generic [ref=e5]:
        - img [ref=e7]
        - generic [ref=e9]: Shifter
      - generic [ref=e10]:
        - generic [ref=e11]:
          - heading "התחברות" [level=1] [ref=e12]
          - paragraph [ref=e13]: Sign in to your workspace
        - generic [ref=e14]:
          - generic [ref=e15]:
            - generic [ref=e16]: דואר אלקטרוני
            - textbox "you@example.com" [ref=e17]: admin@demo.local
          - generic [ref=e18]:
            - generic [ref=e19]: סיסמה
            - generic [ref=e20]:
              - textbox "••••••••" [ref=e21]: Demo1234!
              - button "Show password" [ref=e22] [cursor=pointer]:
                - img [ref=e23]
          - link "שכחת סיסמה?" [ref=e27] [cursor=pointer]:
            - /url: /forgot-password
          - generic [ref=e28]:
            - img [ref=e29]
            - paragraph [ref=e31]: פרטי התחברות שגויים
          - button "כניסה" [ref=e32] [cursor=pointer]
        - paragraph [ref=e33]:
          - text: אין לך חשבון?
          - link "הירשם" [ref=e34] [cursor=pointer]:
            - /url: /register
  - button "Open Next.js Dev Tools" [ref=e40] [cursor=pointer]:
    - img [ref=e41]
  - alert [ref=e44]
```

# Test source

```ts
  1  | import { Page } from "@playwright/test";
  2  | 
  3  | const BASE        = process.env.E2E_BASE_URL   ?? "http://localhost:3000";
  4  | const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? "admin@demo.local";
  5  | const ADMIN_PASS  = process.env.E2E_ADMIN_PASS  ?? "Demo1234!";
  6  | const API_URL     = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
  7  | 
  8  | /**
  9  |  * Log in as the demo admin using structural selectors (locale-agnostic).
  10 |  */
  11 | export async function loginAsAdmin(page: Page): Promise<void> {
  12 |   await page.goto(`${BASE}/login`);
  13 |   await page.locator('input[type="email"]').fill(ADMIN_EMAIL);
  14 |   await page.locator('input[type="password"]').fill(ADMIN_PASS);
  15 |   await page.locator('button[type="submit"]').click();
  16 |   // Wait until we leave /login
> 17 |   await page.waitForFunction(() => !window.location.pathname.startsWith("/login"), { timeout: 20000 });
     |              ^ TimeoutError: page.waitForFunction: Timeout 15000ms exceeded.
  18 | }
  19 | 
  20 | /**
  21 |  * Enter admin mode by fetching the first group via the API, navigating to it,
  22 |  * then clicking the admin mode toggle button.
  23 |  * adminGroupId is NOT persisted in Zustand so this must go through the UI.
  24 |  */
  25 | export async function enterAdminMode(page: Page): Promise<void> {
  26 |   // Grab token + spaceId from localStorage (set during login)
  27 |   const { token, spaceId } = await page.evaluate(() => {
  28 |     const raw = localStorage.getItem("jobuler-space");
  29 |     let spaceId: string | null = null;
  30 |     try { spaceId = raw ? JSON.parse(raw).state?.currentSpaceId : null; } catch { /* ignore */ }
  31 |     return { token: localStorage.getItem("access_token"), spaceId };
  32 |   });
  33 | 
  34 |   if (token && spaceId) {
  35 |     // Fetch groups directly from the API
  36 |     const resp = await page.request.get(`${API_URL}/spaces/${spaceId}/groups`, {
  37 |       headers: { Authorization: `Bearer ${token}` },
  38 |     });
  39 |     if (resp.ok()) {
  40 |       const groups = await resp.json() as Array<{ id: string }>;
  41 |       if (groups.length > 0) {
  42 |         await page.goto(`${BASE}/groups/${groups[0].id}`);
  43 |         await page.waitForURL(/\/groups\/[^/]+$/, { timeout: 10000 });
  44 |         // Click the admin mode button — it's the only button with data-admin-toggle
  45 |         // or we find it by its position in the settings tab area
  46 |         const adminBtn = page.locator("button").filter({ hasText: /admin|ניהול|администрат/i }).first();
  47 |         if (await adminBtn.isVisible({ timeout: 5000 }).catch(() => false)) {
  48 |           await adminBtn.click();
  49 |         }
  50 |         return;
  51 |       }
  52 |     }
  53 |   }
  54 | 
  55 |   // Fallback: navigate to groups page and click first group card
  56 |   await page.goto(`${BASE}/groups`);
  57 |   const firstGroup = page.locator("button").filter({ hasText: /חברים|members|участник/i }).first();
  58 |   if (await firstGroup.isVisible({ timeout: 8000 }).catch(() => false)) {
  59 |     await Promise.all([
  60 |       page.waitForURL(/\/groups\/[^/]+$/, { timeout: 10000 }),
  61 |       firstGroup.click(),
  62 |     ]);
  63 |     const adminBtn = page.locator("button").filter({ hasText: /admin|ניהול|администрат/i }).first();
  64 |     if (await adminBtn.isVisible({ timeout: 5000 }).catch(() => false)) {
  65 |       await adminBtn.click();
  66 |     }
  67 |   }
  68 | }
  69 | 
```