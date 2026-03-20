import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { loginAsAdmin, logout } from './helpers/auth';

const ordersApiAssetId = 'd0000000-0000-0000-0000-000000000001';

test.describe('RH-6 real web flows', () => {
  test('auth web real: route protection, login and logout', async ({ page }) => {
    await page.goto('/services');
    await expect(page).toHaveURL(/\/login$/, { timeout: 15_000 });

    await loginAsAdmin(page);
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible({ timeout: 30_000 });

    await logout(page);
  });

  test('service catalog and source of truth navigate real seeded entities', async ({ page }) => {
    await loginAsAdmin(page);

    await page.goto('/services');
    await expect(page.getByText('Payments Service')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText('Orders Service')).toBeVisible();

    await page.getByRole('link', { name: 'Payments Service' }).first().click();
    await expect(page).toHaveURL(/\/services\/c0000000-0000-0000-0000-000000000002$/);
    await expect(page.getByText('Finance')).toBeVisible({ timeout: 30_000 });

    await page.goto('/source-of-truth');
    await page.getByPlaceholder(/search services, contracts, docs/i).fill('Payments');
    await expect(page.getByRole('link', { name: /payments service/i }).first()).toBeVisible({ timeout: 30_000 });
    await page.getByRole('link', { name: /payments service/i }).first().click();

    await expect(page).toHaveURL(/\/source-of-truth\/services\//);
    await expect(page.getByRole('heading', { name: /payments service/i })).toBeVisible({ timeout: 30_000 });
  });

  test('contracts real: create draft, save spec and submit for review', async ({ page }) => {
    await loginAsAdmin(page);

    await page.goto('/contracts/new');
    await expect(page.getByRole('heading', { name: /create service contract/i })).toBeVisible({ timeout: 30_000 });

    await page.getByRole('button', { name: /rest api/i }).click();
    await page.getByRole('button', { name: /^next$/i }).click();
    await page.getByRole('button', { name: /visual/i }).click();
    await page.getByRole('button', { name: /^next$/i }).click();

    const draftTitle = `RH6 Draft ${randomUUID().slice(0, 8)}`;
    await page.getByLabel(/name/i).fill(draftTitle);
    await page.getByLabel(/description/i).fill('Created by real Playwright RH-6 flow');

    await page.getByRole('button', { name: /create draft/i }).click();
    await expect(page).toHaveURL(/\/contracts\/studio\//, { timeout: 30_000 });

    const specEditor = page.getByPlaceholder(/paste or write your specification content here/i);
    await specEditor.fill(
      [
        'openapi: 3.0.0',
        'info:',
        '  title: RH6 Frontend Draft',
        '  version: 1.0.0',
        'paths:',
        '  /health:',
        '    get:',
        '      summary: Health check',
        '      responses:',
        "        '200':",
        '          description: OK',
      ].join('\n'),
    );

    await page.getByRole('button', { name: /^save$/i }).click();
    await page.getByRole('button', { name: /submit for review/i }).click();

    await expect(page.getByText(/draft submitted for review successfully/i)).toBeVisible({ timeout: 30_000 });
  });

  test('change governance real: list seeded release and open intelligence review', async ({ page }) => {
    await loginAsAdmin(page);

    await page.goto('/releases');
    await page.getByPlaceholder(/api asset id/i).fill(ordersApiAssetId);

    await expect(page.getByText('1.3.0')).toBeVisible({ timeout: 30_000 });
    await page.locator('tbody button').first().click();

    await expect(page.getByText(/change intelligence summary/i)).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(/blast radius/i)).toBeVisible();

    const startReviewButton = page.getByRole('button', { name: /start review/i });
    if (await startReviewButton.isVisible()) {
      await startReviewButton.click();
      await expect(page.getByText(/review/i)).toBeVisible({ timeout: 30_000 });
    }
  });

  test('incidents real: list seeded incidents, open detail and refresh correlation', async ({ page }) => {
    await loginAsAdmin(page);

    await page.goto('/operations/incidents');
    await expect(page.getByText('Payment Gateway — elevated error rate')).toBeVisible({ timeout: 30_000 });

    await page.getByRole('link', { name: /payment gateway — elevated error rate/i }).click();
    await expect(page).toHaveURL(/\/operations\/incidents\//, { timeout: 30_000 });
    await expect(page.getByText('INC-2026-0042')).toBeVisible();

    await page.getByRole('button', { name: /refresh correlation/i }).click();
    await expect(page.getByText(/correlation refreshed|score:/i)).toBeVisible({ timeout: 30_000 });
  });

  test('ai assistant real: create conversation, persist messages, reload and reopen the same conversation', async ({ page }) => {
    await loginAsAdmin(page);

    await page.goto('/ai/assistant');
    await expect(page.getByRole('heading', { name: /ai assistant/i })).toBeVisible({ timeout: 30_000 });

    await page.getByRole('button', { name: /new conversation/i }).first().click();
    const userPrompt = `Summarize the operational risk for Payments Service in production ${randomUUID().slice(0, 8)}`;
    const input = page.getByPlaceholder(/ask about services, contracts, incidents, changes/i);
    await input.fill(userPrompt);
    await page.getByRole('button', { name: /send/i }).click();

    await expect(page.getByText(userPrompt)).toBeVisible({ timeout: 30_000 });
    await expect(
      page.getByText(/degraded response|provider unavailable|grounded|partial context|limited context/i).first(),
    ).toBeVisible({ timeout: 30_000 });

    await page.reload();

    await expect(page).toHaveURL(/conversation=/, { timeout: 30_000 });
    await expect(page.getByText(userPrompt)).toBeVisible({ timeout: 30_000 });
    await expect(
      page.getByText(/degraded response|provider unavailable|grounded|partial context|limited context/i).first(),
    ).toBeVisible({ timeout: 30_000 });
  });
});
