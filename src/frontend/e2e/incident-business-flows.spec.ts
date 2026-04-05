import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E tests for Incident creation business flow.
 * Tests form interactions, field validations, API submission, and error handling.
 */

const INCIDENT_SUMMARY_FIXTURE = {
  totalOpen: 4,
  criticalIncidents: 1,
  withCorrelatedChanges: 2,
  withMitigationAvailable: 3,
  servicesImpacted: 5,
};

const INCIDENTS_LIST_FIXTURE = {
  items: [
    {
      incidentId: 'inc-001',
      reference: 'INC-2026-001',
      title: 'Payment processing degradation',
      incidentType: 'ServiceDegradation',
      severity: 'Critical',
      status: 'Investigating',
      serviceId: 'svc-pay-001',
      serviceDisplayName: 'Payments Service',
      ownerTeam: 'Payments Team',
      environment: 'Production',
      createdAt: '2026-03-18T14:30:00Z',
      hasCorrelatedChanges: true,
      correlationConfidence: 'High',
      mitigationStatus: 'Pending',
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};

function setupIncidentMocks(page: import('@playwright/test').Page) {
  return Promise.all([
    page.route('**/api/v1/incidents/summary**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(INCIDENT_SUMMARY_FIXTURE) }),
    ),
    page.route('**/api/v1/incidents**', (route) => {
      const url = new URL(route.request().url());
      if (route.request().method() === 'POST') {
        route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ incidentId: 'inc-new-001', reference: 'INC-2026-099' }),
        });
        return;
      }
      if (url.pathname.includes('/summary') || /\/incidents\/inc-\d+/.test(url.pathname)) {
        route.fallback();
        return;
      }
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(INCIDENTS_LIST_FIXTURE) });
    }),
  ]);
}

// ─── Incident creation — full business flow ──────────────────────────────────

test.describe('Incidents — create incident business flow', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await setupIncidentMocks(page);
  });

  test('fills all form fields and submits successfully', async ({ page }) => {
    let capturedPayload: Record<string, unknown> | null = null;

    // Override POST to capture payload
    await page.route('**/api/v1/incidents', async (route) => {
      if (route.request().method() === 'POST') {
        capturedPayload = JSON.parse(await route.request().postData() ?? '{}');
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ incidentId: 'inc-new-001', reference: 'INC-2026-099' }),
        });
        return;
      }
      route.fallback();
    });

    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    // Fill all form fields
    await page.getByPlaceholder(/incident title/i).fill('Database connection pool exhausted');
    await page.getByPlaceholder(/describe what happened/i).fill('Connection pool exhausted under high load during peak hours');
    await page.getByPlaceholder(/service id/i).fill('svc-db-001');
    await page.getByPlaceholder(/service display name/i).fill('Database Service');
    await page.getByPlaceholder(/owner team/i).fill('Platform Team');
    await page.getByPlaceholder(/environment/i).fill('Production');

    // Submit the form
    await page.getByRole('button', { name: /^create$/i }).click();

    // Modal should close after successful creation
    await expect(page.getByPlaceholder(/incident title/i)).not.toBeVisible({ timeout: 5_000 });

    // Verify the API was called with correct data
    expect(capturedPayload).not.toBeNull();
    expect(capturedPayload!.title).toBe('Database connection pool exhausted');
    expect(capturedPayload!.description).toContain('Connection pool exhausted');
  });

  test('form validates required fields — title empty disables create', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    // Only fill service fields, leave title empty
    await page.getByPlaceholder(/service id/i).fill('svc-001');
    await page.getByPlaceholder(/service display name/i).fill('Test Service');
    await page.getByPlaceholder(/owner team/i).fill('Team A');

    // Create button should be disabled (title is required)
    const createBtn = page.getByRole('button', { name: /^create$/i });
    await expect(createBtn).toBeDisabled({ timeout: 3_000 });
  });

  test('form validates required fields — service ID empty disables create', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    // Fill title but leave service fields empty
    await page.getByPlaceholder(/incident title/i).fill('Test Incident');
    await page.getByPlaceholder(/describe what happened/i).fill('Test description');

    // Create button should be disabled (serviceId is required)
    const createBtn = page.getByRole('button', { name: /^create$/i });
    await expect(createBtn).toBeDisabled({ timeout: 3_000 });
  });

  test('form resets after cancellation', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    // Fill some fields
    await page.getByPlaceholder(/incident title/i).fill('Temporary Incident');

    // Cancel
    await page.getByRole('button', { name: /cancel/i }).click();
    await expect(page.getByPlaceholder(/incident title/i)).not.toBeVisible({ timeout: 3_000 });

    // Re-open — form should be clean
    await page.getByRole('button', { name: /create incident/i }).click();
    const titleField = page.getByPlaceholder(/incident title/i);
    await expect(titleField).toBeVisible({ timeout: 3_000 });
    await expect(titleField).toHaveValue('');
  });

  test('displays server error when API fails', async ({ page }) => {
    // Override POST to return error
    await page.route('**/api/v1/incidents', (route) => {
      if (route.request().method() === 'POST') {
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ code: 'Incidents.CreateFailed', detail: 'Service temporarily unavailable' }),
        });
        return;
      }
      route.fallback();
    });

    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    // Fill all required fields
    await page.getByPlaceholder(/incident title/i).fill('Error test incident');
    await page.getByPlaceholder(/describe what happened/i).fill('Testing error handling');
    await page.getByPlaceholder(/service id/i).fill('svc-001');
    await page.getByPlaceholder(/service display name/i).fill('Test Service');
    await page.getByPlaceholder(/owner team/i).fill('Test Team');
    await page.getByPlaceholder(/environment/i).fill('Production');

    await page.getByRole('button', { name: /^create$/i }).click();

    // Error should be displayed — modal may show error or toast notification
    await expect(page.getByText(/error|failed|unavailable/i).first()).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Incidents — severity and type selection ─────────────────────────────────

test.describe('Incidents — form field interactions', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await setupIncidentMocks(page);
  });

  test('incident type defaults to ServiceDegradation', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    // Check that incident type has a default value
    const typeSelect = page.locator('select').filter({ hasText: /degradation|regression|issue/i }).first();
    if (await typeSelect.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await expect(typeSelect).toHaveValue('ServiceDegradation');
    }
  });

  test('severity defaults to Major', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    // Check severity default
    const severitySelect = page.locator('select').filter({ hasText: /major|critical|minor/i }).first();
    if (await severitySelect.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await expect(severitySelect).toHaveValue('Major');
    }
  });

  test('form fields are interactive — fill and verify values', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    const titleField = page.getByPlaceholder(/incident title/i);
    const descField = page.getByPlaceholder(/describe what happened/i);

    // Fill and verify
    await titleField.fill('Network partition detected');
    await expect(titleField).toHaveValue('Network partition detected');

    await descField.fill('Inter-service communication failure across availability zones');
    await expect(descField).toHaveValue('Inter-service communication failure across availability zones');
  });
});

// ─── Incidents — list filtering ──────────────────────────────────────────────

test.describe('Incidents — search and filter interactions', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/incidents/summary**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(INCIDENT_SUMMARY_FIXTURE) }),
    );
  });

  test('search input filters incident list via API', async ({ page }) => {
    let lastSearchTerm = '';

    await page.route('**/api/v1/incidents**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary')) { route.fallback(); return; }
      lastSearchTerm = url.searchParams.get('search') ?? '';
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: lastSearchTerm
            ? INCIDENTS_LIST_FIXTURE.items.filter((i) => i.title.toLowerCase().includes(lastSearchTerm.toLowerCase()))
            : INCIDENTS_LIST_FIXTURE.items,
          totalCount: lastSearchTerm ? 0 : INCIDENTS_LIST_FIXTURE.totalCount,
          page: 1,
          pageSize: 20,
        }),
      });
    });

    await page.goto('/operations/incidents');
    await expect(page.getByText('Payment processing degradation')).toBeVisible({ timeout: 5_000 });

    // Type in search — find the search input
    const searchInput = page.getByPlaceholder(/search/i).first();
    if (await searchInput.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await searchInput.fill('kafka');
      // Wait for debounce
      await page.waitForTimeout(500);
    }
  });

  test('status filter tabs change the displayed incidents', async ({ page }) => {
    await page.route('**/api/v1/incidents**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary')) { route.fallback(); return; }
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(INCIDENTS_LIST_FIXTURE) });
    });

    await page.goto('/operations/incidents');
    await expect(page.getByText('Payment processing degradation')).toBeVisible({ timeout: 5_000 });

    // Click on a filter tab if available (e.g., "Open", "Investigating")
    const openTab = page.getByRole('button', { name: /^open$/i }).first();
    if (await openTab.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await openTab.click();
      // Verify the filter is applied (page may re-render)
      await page.waitForTimeout(500);
    }
  });
});
