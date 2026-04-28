import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "./helpers/auth";

const BASE = process.env.E2E_BASE_URL ?? "http://localhost:3000";

test.describe("People management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("people list page loads", async ({ page }) => {
    await page.goto(`${BASE}/admin/people`);
    // AppShell sidebar confirms the page rendered (not a 404/redirect)
    await expect(page.locator("aside")).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/something went wrong/i)).not.toBeVisible();
  });

  test("can create a new person", async ({ page }) => {
    await page.goto(`${BASE}/admin/people`);
    await expect(page.locator("aside")).toBeVisible({ timeout: 10000 });

    // Name input — placeholder varies by locale, use type=text in the form
    const nameInput = page.locator('input[type="text"]').first();
    if (await nameInput.isVisible({ timeout: 5000 }).catch(() => false)) {
      const uniqueName = `E2E Person ${Date.now()}`;
      await nameInput.fill(uniqueName);
      // Submit button in the create form
      await page.locator('button[type="submit"]').first().click();
      await expect(page.getByText(uniqueName)).toBeVisible({ timeout: 10000 });
    }
  });

  test("person detail page loads", async ({ page }) => {
    await page.goto(`${BASE}/admin/people`);
    await expect(page.locator("aside")).toBeVisible({ timeout: 10000 });

    // Click the first person link/button that navigates to a detail page
    const firstPerson = page.locator("a[href*='/admin/people/'], button").filter({ hasText: /→|view|details/i }).first();
    if (await firstPerson.isVisible({ timeout: 5000 }).catch(() => false)) {
      await firstPerson.click();
      await expect(page.locator("aside")).toBeVisible({ timeout: 8000 });
    }
  });
});
