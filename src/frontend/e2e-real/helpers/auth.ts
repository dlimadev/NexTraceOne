import { expect, type Page } from '@playwright/test';

export async function loginAsAdmin(page: Page): Promise<void> {
  await page.goto('/login');

  await page.getByLabel(/email/i).fill('admin@nextraceone.dev');
  await page.getByLabel(/password/i).fill('Admin@123');
  await page.locator('form').getByRole('button', { name: /^sign in$/i }).click();

  await expect(page).toHaveURL(/\/$/, { timeout: 30_000 });
}

export async function logout(page: Page): Promise<void> {
  await page.getByLabel(/sign out/i).first().click();
  await expect(page).toHaveURL(/\/login$/, { timeout: 30_000 });
}

