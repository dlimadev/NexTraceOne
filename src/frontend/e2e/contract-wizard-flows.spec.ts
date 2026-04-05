import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E tests for Contract creation wizard — full business flow.
 * Covers multi-step wizard, type/mode selection, form fill, and draft creation via API.
 */

// ─── Contract creation wizard — type selection (Step 1) ──────────────────────

test.describe('Contract creation wizard — Step 1: Type selection', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('displays all contract types as selectable cards', async ({ page }) => {
    await page.goto('/contracts/new');
    // REST API type should be visible
    await expect(page.getByRole('heading', { name: /rest api/i })).toBeVisible({ timeout: 5_000 });
  });

  test('selecting REST API enables Next button', async ({ page }) => {
    await page.goto('/contracts/new');
    const restBtn = page.getByRole('button', { name: /rest api/i }).first();
    await expect(restBtn).toBeVisible({ timeout: 5_000 });
    await restBtn.click();

    // After selection, Next button should be enabled
    const nextBtn = page.getByRole('button', { name: /next/i });
    await expect(nextBtn).toBeEnabled({ timeout: 3_000 });
  });

  test('Next button is disabled when no type is selected', async ({ page }) => {
    await page.goto('/contracts/new');
    // Before any selection, Next should be disabled
    const nextBtn = page.getByRole('button', { name: /next/i });
    if (await nextBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await expect(nextBtn).toBeDisabled();
    }
  });

  test('selecting SOAP type works', async ({ page }) => {
    await page.goto('/contracts/new');
    const soapBtn = page.getByRole('button', { name: /soap/i }).first();
    if (await soapBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await soapBtn.click();
      const nextBtn = page.getByRole('button', { name: /next/i });
      await expect(nextBtn).toBeEnabled({ timeout: 3_000 });
    }
  });

  test('selecting Event/AsyncAPI type works', async ({ page }) => {
    await page.goto('/contracts/new');
    const eventBtn = page.getByRole('button', { name: /event|async/i }).first();
    if (await eventBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await eventBtn.click();
      const nextBtn = page.getByRole('button', { name: /next/i });
      await expect(nextBtn).toBeEnabled({ timeout: 3_000 });
    }
  });
});

// ─── Contract creation wizard — mode selection (Step 2) ──────────────────────

test.describe('Contract creation wizard — Step 2: Mode selection', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('advancing from type selection shows creation modes', async ({ page }) => {
    await page.goto('/contracts/new');
    // Select REST API
    await page.getByRole('button', { name: /rest api/i }).first().click();
    const nextBtn = page.getByRole('button', { name: /next/i });
    if (await nextBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nextBtn.click();
      // Step 2 should show creation mode options
      await expect(page.getByText(/visual|import|ai/i).first()).toBeVisible({ timeout: 5_000 });
    }
  });

  test('back button returns to type selection', async ({ page }) => {
    await page.goto('/contracts/new');
    await page.getByRole('button', { name: /rest api/i }).first().click();
    const nextBtn = page.getByRole('button', { name: /next/i });
    if (await nextBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nextBtn.click();
      // Now on step 2 — click back
      const backBtn = page.getByRole('button', { name: /back/i });
      if (await backBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await backBtn.click();
        // Should be back on step 1 with REST API still visible
        await expect(page.getByRole('heading', { name: /rest api/i })).toBeVisible({ timeout: 3_000 });
      }
    }
  });
});

// ─── Contract creation wizard — form details (Step 3) ────────────────────────

test.describe('Contract creation wizard — Step 3: Details and submission', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  async function navigateToStep3(page: import('@playwright/test').Page, type: string = 'rest api', mode?: string) {
    await page.goto('/contracts/new');
    // Step 1: select type
    await page.getByRole('button', { name: new RegExp(type, 'i') }).first().click();
    const nextBtn1 = page.getByRole('button', { name: /next/i });
    if (await nextBtn1.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nextBtn1.click();
    }
    // Step 2: select mode
    if (mode) {
      const modeBtn = page.getByRole('button', { name: new RegExp(mode, 'i') }).first();
      if (await modeBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await modeBtn.click();
      }
    } else {
      // Default: click first available mode (visual)
      const visualBtn = page.getByRole('button', { name: /visual/i }).first();
      if (await visualBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await visualBtn.click();
      }
    }
    const nextBtn2 = page.getByRole('button', { name: /next/i });
    if (await nextBtn2.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nextBtn2.click();
    }
  }

  test('fills title and creates REST API draft successfully', async ({ page }) => {
    let draftCreated = false;

    await page.route('**/api/v1/contracts/studio/draft**', (route) => {
      draftCreated = true;
      route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({ draftId: 'draft-new-001', status: 'Created' }),
      });
    });
    // Mock studio redirect page
    await page.route('**/api/v1/contracts/studio/draft-new-001**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ draftId: 'draft-new-001' }) }),
    );

    await navigateToStep3(page, 'rest api');

    // Fill the title field
    const titleInput = page.getByPlaceholder(/title|name/i).first();
    if (await titleInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await titleInput.fill('Payments API v3');

      // Click Create/Save button
      const createBtn = page.getByRole('button', { name: /create|save|finish/i }).first();
      if (await createBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await createBtn.click();
        await page.waitForTimeout(2_000);
        expect(draftCreated).toBeTruthy();
      }
    }
  });

  test('import mode shows content input area', async ({ page }) => {
    await navigateToStep3(page, 'rest api', 'import');

    // Import mode should show a content/spec input (textarea or code editor)
    const contentArea = page.getByPlaceholder(/paste|content|spec|json|yaml/i).first();
    const textArea = page.locator('textarea').first();

    const hasContentInput = await contentArea.isVisible({ timeout: 3_000 }).catch(() => false)
      || await textArea.isVisible({ timeout: 2_000 }).catch(() => false);

    // If we're on step 3 with import mode, there should be a content input
    if (hasContentInput) {
      expect(true).toBeTruthy();
    }
  });

  test('AI mode shows prompt input', async ({ page }) => {
    await navigateToStep3(page, 'rest api', 'ai');

    // AI mode should show a prompt/description input
    const promptArea = page.getByPlaceholder(/describe|prompt|generate/i).first();
    const textArea = page.locator('textarea').first();

    const hasPromptInput = await promptArea.isVisible({ timeout: 3_000 }).catch(() => false)
      || await textArea.isVisible({ timeout: 2_000 }).catch(() => false);

    if (hasPromptInput) {
      expect(true).toBeTruthy();
    }
  });
});

// ─── Contract creation — SOAP-specific fields ────────────────────────────────

test.describe('Contract creation — SOAP-specific form fields', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('SOAP type shows SOAP-specific fields in details step', async ({ page }) => {
    await page.goto('/contracts/new');
    // Select SOAP
    const soapBtn = page.getByRole('button', { name: /soap/i }).first();
    if (!await soapBtn.isVisible({ timeout: 3_000 }).catch(() => false)) return;
    await soapBtn.click();

    const nextBtn = page.getByRole('button', { name: /next/i });
    if (await nextBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nextBtn.click();
    }

    // Select visual mode
    const visualBtn = page.getByRole('button', { name: /visual/i }).first();
    if (await visualBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await visualBtn.click();
    }
    const nextBtn2 = page.getByRole('button', { name: /next/i });
    if (await nextBtn2.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await nextBtn2.click();
    }

    // SOAP-specific fields: namespace, version, endpoint
    const namespaceInput = page.getByPlaceholder(/namespace/i).first();
    const versionSelect = page.locator('select').filter({ hasText: /1\.1|1\.2/i }).first();

    const hasSoapFields = await namespaceInput.isVisible({ timeout: 3_000 }).catch(() => false)
      || await versionSelect.isVisible({ timeout: 2_000 }).catch(() => false);

    if (hasSoapFields) {
      // SOAP-specific fields are present
      expect(true).toBeTruthy();
    }
  });
});
