import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { getAdminAuthHeaders, loginAsAdmin, loginAsAuditor, logout } from './helpers/auth';

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

  test('contracts real: create draft, save, reload, reopen, publish and open workspace', async ({ page, request }) => {
    await loginAsAdmin(page);
    const authHeaders = await getAdminAuthHeaders(request);

    await page.goto('/contracts');
    await expect(page.getByRole('heading', { name: /contract catalog/i })).toBeVisible({ timeout: 30_000 });

    await page.goto('/contracts/new');
    await expect(page.getByRole('heading', { name: /create service contract/i })).toBeVisible({ timeout: 30_000 });

    await page.getByRole('button', { name: /rest api/i }).click();
    await page.getByRole('button', { name: /^next$/i }).click();
    await page.getByRole('button', { name: /visual/i }).click();
    await page.getByRole('button', { name: /^next$/i }).click();

    const suffix = randomUUID().slice(0, 8);
    const initialTitle = `RH6 Draft ${suffix}`;
    const updatedTitle = `${initialTitle} Updated`;

    await page.locator('input[type="text"]').first().fill(initialTitle);
    await page.locator('textarea').first().fill('Created by real Playwright RH-6 flow');
    await page.locator('select').first().selectOption({ label: 'Payments Service' });

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
    await expect(page.getByText(/changes saved successfully/i)).toBeVisible({ timeout: 30_000 });

    await page.getByRole('button', { name: /metadata/i }).click();
    await page.locator('input[type="text"]').first().fill(updatedTitle);
    await page.locator('textarea').first().fill('Updated by real Playwright RH-6 flow');
    await page.locator('input[type="text"]').nth(1).fill('1.0.1');
    await page.getByRole('button', { name: /^save$/i }).click();
    await expect(page.getByText(/changes saved successfully/i)).toBeVisible({ timeout: 30_000 });

    const draftUrl = page.url();
    const draftId = draftUrl.split('/').pop();
    if (!draftId) {
      throw new Error(`Could not extract draft id from ${draftUrl}`);
    }

    await page.reload();
    await expect(page).toHaveURL(new RegExp(`/contracts/studio/${draftId}$`), { timeout: 30_000 });
    await expect(page.getByRole('heading', { name: updatedTitle })).toBeVisible({ timeout: 30_000 });

    await page.getByRole('button', { name: /submit for review/i }).click();
    await expect(page.getByText(/draft submitted for review successfully/i)).toBeVisible({ timeout: 30_000 });

    const contractVersionId = (() => request.post(`/api/v1/contracts/drafts/${draftId}/approve`, {
      headers: authHeaders,
      data: {
        approvedBy: 'admin@nextraceone.dev',
        comment: 'Approved by real Playwright RH-6 flow',
      },
    }).then(async (approveResponse) => {
      expect(approveResponse.ok()).toBeTruthy();

      const publishResponse = await request.post(`/api/v1/contracts/drafts/${draftId}/publish`, {
        headers: authHeaders,
        data: { publishedBy: 'admin@nextraceone.dev' },
      });
      expect(publishResponse.ok()).toBeTruthy();

      const payload = await publishResponse.json() as Record<string, unknown>;
      const root = (payload.data as Record<string, unknown> | undefined) ?? payload;
      const publishedContractVersionId = (root.contractVersionId as string | undefined) ?? (root.ContractVersionId as string | undefined);
      expect(publishedContractVersionId).toBeTruthy();
      return publishedContractVersionId as string;
    }))();

    await page.goto(`/contracts/${await contractVersionId}`);
    await expect(page.getByRole('heading', { name: updatedTitle })).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText('Finance')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText('Payments Service')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText('1.0.1')).toBeVisible({ timeout: 30_000 });
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

  test('incidents real: list, create, persist, open detail, reload and reopen', async ({ page }) => {
    await loginAsAdmin(page);

    const title = `ZR4 Incident ${randomUUID().slice(0, 8)}`;
    const serviceId = `svc-zr4-${randomUUID().slice(0, 6)}`;

    await page.goto('/operations/incidents');
    await expect(page.getByText('Payment Gateway — elevated error rate')).toBeVisible({ timeout: 30_000 });

    await page.getByRole('button', { name: /create incident/i }).click();
    await page.getByPlaceholder(/incident title/i).fill(title);
    await page.getByPlaceholder(/service id/i).fill(serviceId);
    await page.getByPlaceholder(/service display name/i).fill('ZR4 Incidents Service');
    await page.getByPlaceholder(/owner team/i).fill('platform-core');
    await page.getByPlaceholder(/describe what happened/i).fill('Created by real ZR-4 browser flow');
    await page.getByRole('button', { name: /^create$/i }).click();

    await expect(page.getByText(/was created and persisted successfully/i)).toBeVisible({ timeout: 30_000 });
    await expect(page.getByRole('link', { name: /open incident detail/i })).toBeVisible();
    await expect(page.getByRole('link', { name: new RegExp(title, 'i') })).toBeVisible({ timeout: 30_000 });

    await page.getByRole('link', { name: /open incident detail/i }).click();
    await expect(page).toHaveURL(/\/operations\/incidents\//, { timeout: 30_000 });
    await expect(page.getByText(title)).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText('platform-core')).toBeVisible();
    await expect(page.getByText('ZR4 Incidents Service')).toBeVisible();

    await page.reload();
    await expect(page.getByText(title)).toBeVisible({ timeout: 30_000 });

    await page.getByRole('link', { name: /back to incidents/i }).click();
    await expect(page).toHaveURL(/\/operations\/incidents$/, { timeout: 30_000 });
    await expect(page.getByRole('link', { name: new RegExp(title, 'i') })).toBeVisible({ timeout: 30_000 });
  });

  test('incidents real: read-only user can list incidents but cannot create them', async ({ page }) => {
    await loginAsAuditor(page);

    await page.goto('/operations/incidents');
    await expect(page.getByText('Payment Gateway — elevated error rate')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(/cannot create new ones/i)).toBeVisible({ timeout: 30_000 });
    await expect(page.getByRole('button', { name: /create incident/i })).toHaveCount(0);
  });

  test('ai assistant real: create conversation, persist messages, reload and reopen the same conversation', async ({ page }) => {
    await loginAsAdmin(page);

    await page.goto('/ai/assistant');
    await expect(page.getByRole('heading', { name: /ai assistant/i })).toBeVisible({ timeout: 30_000 });

    await page.getByRole('button', { name: /new conversation/i }).first().click();
    const firstPrompt = `Summarize the operational risk for Payments Service in production ${randomUUID().slice(0, 8)}`;
    const input = page.getByPlaceholder(/ask about services, contracts, incidents, changes/i);
    await input.fill(firstPrompt);
    await page.getByRole('button', { name: /send/i }).click();

    await expect(page.getByText(firstPrompt)).toBeVisible({ timeout: 30_000 });
    await expect(page).toHaveURL(/conversation=/, { timeout: 30_000 });
    await expect(
      page.getByText(/degraded response|provider unavailable|grounded|partial context|limited context/i).first(),
    ).toBeVisible({ timeout: 30_000 });

    const persistedConversationUrl = page.url();
    await page.reload();

    await expect(page).toHaveURL(persistedConversationUrl, { timeout: 30_000 });
    await expect(page.getByText(firstPrompt)).toBeVisible({ timeout: 30_000 });

    const secondPrompt = `Continue the same conversation ${randomUUID().slice(0, 8)}`;
    await input.fill(secondPrompt);
    await page.getByRole('button', { name: /send/i }).click();

    await expect(page.getByText(secondPrompt)).toBeVisible({ timeout: 30_000 });
    await page.reload();

    await expect(page.getByText(firstPrompt)).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(secondPrompt)).toBeVisible({ timeout: 30_000 });
  });

  test('audit real: list, search and verify integrity with real backend data', async ({ page, request }) => {
    const authHeaders = await getAdminAuthHeaders(request);

    const initialSearchResponse = await request.get('/api/v1/audit/search?page=1&pageSize=20', {
      headers: authHeaders,
    });
    expect(initialSearchResponse.ok()).toBeTruthy();

    const initialSearchPayload = await initialSearchResponse.json() as { items?: Array<{ sourceModule?: string; actionType?: string }> };
    const firstItem = initialSearchPayload.items?.[0];
    expect(firstItem?.actionType).toBeTruthy();
    expect(firstItem?.sourceModule).toBeTruthy();

    await loginAsAdmin(page);
    await page.goto('/audit');

    await expect(page.getByRole('heading', { name: /audit log/i })).toBeVisible({ timeout: 30_000 });
    await page.getByPlaceholder(/filter by event type/i).fill(firstItem!.actionType!);
    await expect(page.getByText(firstItem!.actionType!)).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(firstItem!.sourceModule!)).toBeVisible({ timeout: 30_000 });

    await page.getByRole('button', { name: /verify integrity/i }).click();
    await expect(page.getByText(/hash chain is valid|integrity violation detected/i)).toBeVisible({ timeout: 30_000 });
  });
});
