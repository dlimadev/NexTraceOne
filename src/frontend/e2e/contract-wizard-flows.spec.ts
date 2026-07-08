import { test, expect, type Page } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E do wizard de criação de contrato — fluxo v5.
 *
 * O wizard tem 4 passos: service → typeMode (tipo + modo) → details → confirm.
 * Os tipos de contrato disponíveis dependem do serviço escolhido
 * (allowedContractTypes). Usamos um serviço 'ThirdParty' que permite
 * RestApi + Soap + Event, para exercitar os três tipos.
 */

const WIZARD_SERVICES = {
  items: [
    {
      serviceId: 'svc-int-001',
      name: 'integration-hub',
      displayName: 'Integration Hub',
      serviceType: 'ThirdParty',
      domain: 'Platform',
      teamName: 'Platform Team',
      criticality: 'Medium',
      lifecycleStatus: 'Active',
      exposureType: 'External',
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};

async function mockServices(page: Page) {
  await page.route('**/api/v1/catalog/services**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(WIZARD_SERVICES),
    }),
  );
}

/** Navega de /contracts/new até ao passo typeMode (serviço já escolhido). */
async function gotoTypeStep(page: Page) {
  await mockServices(page);
  await page.goto('/contracts/new');
  await page.getByRole('button', { name: /Integration Hub/i }).click();
  await page.getByRole('button', { name: /next/i }).click();
}

// ─── Passo Type & Mode — seleção de tipo ─────────────────────────────────────

test.describe('Contract creation wizard — type selection', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('lista os tipos de contrato permitidos pelo serviço', async ({ page }) => {
    await gotoTypeStep(page);
    await expect(page.getByRole('button', { name: /rest api/i }).first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByRole('button', { name: /soap/i }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: /event/i }).first()).toBeVisible();
  });

  test('selecionar REST API revela a secção de modo de criação', async ({ page }) => {
    await gotoTypeStep(page);
    await page.getByRole('button', { name: /rest api/i }).first().click();
    await expect(page.getByText(/how do you want to create it/i)).toBeVisible({ timeout: 3_000 });
  });

  test('a secção de modo está oculta até um tipo ser selecionado', async ({ page }) => {
    await gotoTypeStep(page);
    // Garantir que o passo typeMode carregou.
    await expect(page.getByRole('button', { name: /rest api/i }).first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(/how do you want to create it/i)).toHaveCount(0);
  });

  test('selecionar SOAP funciona', async ({ page }) => {
    await gotoTypeStep(page);
    await page.getByRole('button', { name: /soap/i }).first().click();
    await expect(page.getByText(/how do you want to create it/i)).toBeVisible({ timeout: 3_000 });
  });

  test('selecionar Event/AsyncAPI funciona', async ({ page }) => {
    await gotoTypeStep(page);
    await page.getByRole('button', { name: /event/i }).first().click();
    await expect(page.getByText(/how do you want to create it/i)).toBeVisible({ timeout: 3_000 });
  });
});

// ─── Passo Type & Mode — seleção de modo ─────────────────────────────────────

test.describe('Contract creation wizard — mode selection', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('após escolher o tipo aparecem os modos de criação', async ({ page }) => {
    await gotoTypeStep(page);
    await page.getByRole('button', { name: /rest api/i }).first().click();
    await expect(page.getByRole('button', { name: /visual builder/i })).toBeVisible({ timeout: 5_000 });
    await expect(page.getByRole('button', { name: /import/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /ai generation/i })).toBeVisible();
  });

  test('o botão Anterior volta ao passo de seleção de serviço', async ({ page }) => {
    await gotoTypeStep(page);
    await page.getByRole('button', { name: /back/i }).click();
    await expect(page.getByRole('button', { name: /Integration Hub/i })).toBeVisible({ timeout: 3_000 });
  });
});

// ─── Passo Details + confirmação ─────────────────────────────────────────────

test.describe('Contract creation wizard — details and submission', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  async function navigateToDetails(page: Page, type = 'rest api', mode = 'visual builder') {
    await gotoTypeStep(page);
    await page.getByRole('button', { name: new RegExp(type, 'i') }).first().click();
    await page.getByRole('button', { name: new RegExp(mode, 'i') }).first().click();
    await page.getByRole('button', { name: /next/i }).click();
  }

  test('preenche o nome e chega ao passo de confirmação', async ({ page }) => {
    await navigateToDetails(page, 'rest api', 'visual builder');
    const nameInput = page.getByPlaceholder(/user management api/i).first();
    await expect(nameInput).toBeVisible({ timeout: 5_000 });
    await nameInput.fill('Payments API v3');
    await page.getByRole('button', { name: /next/i }).click();
    // Passo confirm: cabeçalho de revisão ("Review & create").
    await expect(page.getByText(/review & create/i)).toBeVisible({ timeout: 5_000 });
  });

  test('modo Import mostra uma área de conteúdo (textarea)', async ({ page }) => {
    await navigateToDetails(page, 'rest api', 'import');
    await expect(page.locator('textarea').first()).toBeVisible({ timeout: 5_000 });
  });

  test('modo AI mostra uma área de descrição (textarea)', async ({ page }) => {
    await navigateToDetails(page, 'rest api', 'ai generation');
    await expect(page.locator('textarea').first()).toBeVisible({ timeout: 5_000 });
  });
});

// ─── SOAP-specific ───────────────────────────────────────────────────────────

test.describe('Contract creation — SOAP-specific flow', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('tipo SOAP avança pelo wizard até ao passo de details', async ({ page }) => {
    await gotoTypeStep(page);
    await page.getByRole('button', { name: /soap/i }).first().click();
    await page.getByRole('button', { name: /visual builder/i }).first().click();
    await page.getByRole('button', { name: /next/i }).click();
    await expect(page.getByPlaceholder(/user management api/i).first()).toBeVisible({ timeout: 5_000 });
  });
});
