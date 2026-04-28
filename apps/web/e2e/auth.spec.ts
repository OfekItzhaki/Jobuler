import { test, expect } from "@playwright/test";

const BASE        = process.env.E2E_BASE_URL   ?? "http://localhost:3000";
const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? "admin@demo.local";
const ADMIN_PASS  = process.env.E2E_ADMIN_PASS  ?? "Demo1234!";

test.describe("Authentication", () => {
  test("login page renders", async ({ page }) => {
    await page.goto(`${BASE}/login`);
    await expect(page.locator('input[type="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"]')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test("invalid credentials shows error", async ({ page }) => {
    await page.goto(`${BASE}/login`);
    await page.locator('input[type="email"]').fill("wrong@example.com");
    await page.locator('input[type="password"]').fill("wrongpassword");
    await page.locator('button[type="submit"]').click();
    // Stay on login page — don't redirect
    await page.waitForTimeout(3000);
    await expect(page).toHaveURL(/login/);
    // An error element appears (red background div)
    await expect(page.locator("form").locator("div").filter({ hasText: /.{3,}/ }).last()).toBeVisible({ timeout: 8000 });
  });

  test("valid credentials redirects away from login", async ({ page }) => {
    await page.goto(`${BASE}/login`);
    await page.locator('input[type="email"]').fill(ADMIN_EMAIL);
    await page.locator('input[type="password"]').fill(ADMIN_PASS);
    await page.locator('button[type="submit"]').click();
    await expect(page).not.toHaveURL(/\/login/, { timeout: 15000 });
  });
});
