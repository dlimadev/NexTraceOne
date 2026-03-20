import { expect, type APIRequestContext, type Page } from '@playwright/test';

async function getBearerToken(request: APIRequestContext, email: string, password: string): Promise<string> {
  const response = await request.post('/api/v1/identity/auth/login', {
    data: { Email: email, Password: password },
  });

  expect(response.ok()).toBeTruthy();
  const payload = await response.json() as Record<string, unknown>;
  const nested = (payload.data as Record<string, unknown> | undefined) ?? payload;
  const token = nested.accessToken;

  expect(typeof token).toBe('string');
  return token as string;
}

export async function getAdminAuthHeaders(request: APIRequestContext): Promise<Record<string, string>> {
  const token = await getBearerToken(request, 'admin@nextraceone.dev', 'Admin@123');
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}

export async function loginAsAdmin(page: Page): Promise<void> {
  await page.goto('/login');

  await page.getByLabel(/email/i).fill('admin@nextraceone.dev');
  await page.getByLabel(/password/i).fill('Admin@123');
  await page.locator('form').getByRole('button', { name: /^sign in$/i }).click();

  await expect(page).toHaveURL(/\/$/, { timeout: 30_000 });
}

export async function loginAsAuditor(page: Page): Promise<void> {
  await page.goto('/login');

  await page.getByLabel(/email/i).fill('auditor@nextraceone.dev');
  await page.getByLabel(/password/i).fill('Admin@123');
  await page.locator('form').getByRole('button', { name: /^sign in$/i }).click();

  await expect(page).toHaveURL(/\/$/, { timeout: 30_000 });
}

export async function logout(page: Page): Promise<void> {
  await page.getByLabel(/sign out/i).first().click();
  await expect(page).toHaveURL(/\/login$/, { timeout: 30_000 });
}

