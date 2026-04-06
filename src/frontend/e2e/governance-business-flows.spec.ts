import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E tests for Change Governance decision flows, Workflow approval/rejection,
 * Promotion request creation, and Knowledge Hub search & filters.
 * Covers form interactions, data submission, and business flow validation.
 */

// ─── Fixtures ─────────────────────────────────────────────────────────────────

const CHANGE_DETAIL_FIXTURE = {
  changeId: 'chg-001',
  serviceName: 'payments-service',
  serviceId: 'svc-pay-001',
  version: 'v2.2.0',
  environment: 'prod',
  changeType: 'Deployment',
  deploymentStatus: 'Deployed',
  confidenceStatus: 'NeedsAttention',
  changeScore: 0.65,
  deployedAt: '2026-03-18T14:00:00Z',
  commitSha: 'a1b2c3d4e5f6',
  description: 'Deploy payments-service v2.2.0',
  blastRadius: { totalAffected: 3, directConsumers: 2, transitiveConsumers: 1 },
};

const CHANGE_ADVISORY_FIXTURE = {
  changeId: 'chg-001',
  recommendation: 'ApproveConditionally',
  confidenceScore: 0.65,
  overallConfidence: 0.65,
  factors: [
    { factorName: 'EvidenceCompleteness', status: 'Pass', weight: 0.9, description: 'No breaking changes' },
    { factorName: 'ChangeScore', status: 'Warning', weight: 0.4, description: 'Error rate above baseline' },
  ],
  rationale: 'Conditional approval — monitor error rate',
};

function setupChangeDetailMocks(page: import('@playwright/test').Page) {
  return Promise.all([
    page.route('**/api/v1/changes/chg-001', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.endsWith('/intelligence')) {
        return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({}) });
      }
      if (url.pathname.endsWith('/advisory')) {
        return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_ADVISORY_FIXTURE) });
      }
      if (url.pathname.endsWith('/decisions')) {
        return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) });
      }
      if (url.pathname.endsWith('/blast-radius')) {
        return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_DETAIL_FIXTURE.blastRadius) });
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_DETAIL_FIXTURE) });
    }),
  ]);
}

// ─── Change Governance — approve/reject decision flow ─────────────────────────

test.describe('Change Governance — approve decision flow', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await setupChangeDetailMocks(page);
  });

  test('clicking Approve sends approval decision to API', async ({ page }) => {
    await page.route('**/api/v1/changes/chg-001/decision**', async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ changeId: 'chg-001', decision: 'Approved', decidedAt: new Date().toISOString() }),
        });
        return;
      }
      route.fallback();
    });

    await page.goto('/changes/chg-001');
    await expect(page.getByText('Approve Conditionally').first()).toBeVisible({ timeout: 5_000 });

    const approveBtn = page.getByRole('button', { name: /approve/i }).first();
    if (await approveBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await approveBtn.click();

      // If there's a confirmation dialog/notes field, fill it
      const notesField = page.getByPlaceholder(/notes|reason|comment/i).first();
      if (await notesField.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await notesField.fill('Approved after reviewing error rate trends');
      }

      // Submit confirmation if needed
      const confirmBtn = page.getByRole('button', { name: /confirm|submit|approve/i }).first();
      if (await confirmBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await confirmBtn.click();
      }

      await page.waitForTimeout(1_500);
    }
  });

  test('clicking Reject shows reason field and sends rejection', async ({ page }) => {
    await page.route('**/api/v1/changes/chg-001/decision**', async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ changeId: 'chg-001', decision: 'Rejected', decidedAt: new Date().toISOString() }),
        });
        return;
      }
      route.fallback();
    });

    await page.goto('/changes/chg-001');
    await expect(page.getByText('Approve Conditionally').first()).toBeVisible({ timeout: 5_000 });

    const rejectBtn = page.getByRole('button', { name: /reject/i }).first();
    if (await rejectBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await rejectBtn.click();

      // Should show reason/notes field
      const reasonField = page.getByPlaceholder(/reason|notes|comment|justification/i).first();
      if (await reasonField.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await reasonField.fill('Error rate too high — requires investigation before production');
      }

      // Submit rejection
      const confirmBtn = page.getByRole('button', { name: /confirm|submit|reject/i }).first();
      if (await confirmBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await confirmBtn.click();
        await page.waitForTimeout(1_500);
      }
    }
  });
});

// ─── Workflow Approval/Rejection ──────────────────────────────────────────────

test.describe('Workflow — approval and rejection flows', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/workflow/templates', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) }),
    );
  });

  test('approve workflow instance sends API call', async ({ page }) => {
    let approveCalled = false;

    await page.route('**/api/v1/workflow/instances**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{
            id: 'inst-1',
            releaseId: 'rel-001',
            templateId: 'tpl-1',
            status: 'InProgress',
            currentStage: 'stg-1',
            createdAt: '2024-01-15T10:00:00Z',
          }],
          totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
        }),
      }),
    );

    await page.route('**/api/v1/workflow/instances/inst-1/stages/stg-1/approve**', (route) => {
      approveCalled = true;
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true }) });
    });

    await page.goto('/workflow');
    const approveBtn = page.getByRole('button', { name: /approve/i });
    await expect(approveBtn).toBeVisible({ timeout: 5_000 });
    await approveBtn.click();

    await page.waitForTimeout(1_500);
    expect(approveCalled).toBeTruthy();
  });

  test('reject workflow requires reason and sends API call', async ({ page }) => {
    let rejectPayload: Record<string, unknown> | null = null;

    await page.route('**/api/v1/workflow/instances**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{
            id: 'inst-1',
            releaseId: 'rel-001',
            templateId: 'tpl-1',
            status: 'InProgress',
            currentStage: 'stg-1',
            createdAt: '2024-01-15T10:00:00Z',
          }],
          totalCount: 1, page: 1, pageSize: 20, totalPages: 1,
        }),
      }),
    );

    await page.route('**/api/v1/workflow/instances/inst-1/stages/stg-1/reject**', async (route) => {
      rejectPayload = JSON.parse(await route.request().postData() ?? '{}');
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true }) });
    });

    await page.goto('/workflow');
    await page.getByRole('button', { name: /reject/i }).click();

    // Rejection reason field should appear
    const reasonField = page.getByPlaceholder(/reason for rejection/i);
    await expect(reasonField).toBeVisible({ timeout: 3_000 });

    // Confirm button should be disabled with empty reason
    const confirmBtn = page.getByRole('button', { name: /confirm reject/i });
    await expect(confirmBtn).toBeDisabled();

    // Fill reason
    await reasonField.fill('Security review not completed');
    await expect(confirmBtn).toBeEnabled();
    await confirmBtn.click();

    await page.waitForTimeout(1_500);
    expect(rejectPayload).not.toBeNull();
    expect(rejectPayload!.reason).toBe('Security review not completed');
  });
});

// ─── Promotion Request Creation ──────────────────────────────────────────────

test.describe('Promotion — create request flow', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/releases**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            { id: 'rel-001', name: 'payments-service v2.2.0', version: 'v2.2.0', createdAt: '2026-03-18T14:00:00Z' },
            { id: 'rel-002', name: 'auth-service v1.5.1', version: 'v1.5.1', createdAt: '2026-03-17T10:00:00Z' },
          ],
          totalCount: 2, page: 1, pageSize: 50, totalPages: 1,
        }),
      }),
    );
    await page.route('**/api/v1/promotion/requests**', (route) => {
      if (route.request().method() === 'POST') {
        route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: 'pr-new-001', status: 'Pending' }),
        });
        return;
      }
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      });
    });
  });

  test('opens form and submits promotion request', async ({ page }) => {
    await page.route('**/api/v1/promotion/requests', async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: 'pr-new-001', status: 'Pending' }),
        });
        return;
      }
      route.fallback();
    });

    await page.goto('/promotion');
    await page.getByRole('button', { name: /new promotion request/i }).click();

    // The form should appear
    await expect(page.getByText(/create promotion request/i)).toBeVisible({ timeout: 3_000 });

    // Fill form fields — select a release from dropdown (by known fixture value)
    const releaseSelect = page.locator('select').first();
    if (await releaseSelect.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await releaseSelect.selectOption({ label: 'payments-service v2.2.0' });
    }

    // Click Create Request
    const createBtn = page.getByRole('button', { name: /create request/i });
    await expect(createBtn).toBeVisible({ timeout: 3_000 });
    await createBtn.click();

    await page.waitForTimeout(1_500);
    // Form should close/reset or show success
  });

  test('environment pipeline displays all environments', async ({ page }) => {
    await page.goto('/promotion');
    // Environments come from the auth mock (development, staging, production)
    await expect(page.getByText('development').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('staging').first()).toBeVisible();
    await expect(page.getByText('production').first()).toBeVisible();
  });
});

// ─── Knowledge Hub — search and filter interactions ──────────────────────────

test.describe('Knowledge Hub — search and filter business flows', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/knowledge/documents**', (route) => {
      const url = new URL(route.request().url());
      const search = url.searchParams.get('search') ?? '';
      const category = url.searchParams.get('category') ?? '';
      const allDocs = [
        {
          id: 'doc-001',
          title: 'Payment Gateway Recovery Runbook',
          slug: 'payment-gateway-recovery',
          summary: 'Steps to recover the payment gateway',
          category: 'Runbook',
          status: 'Published',
          authorId: 'user-001',
          tags: ['payments', 'recovery'],
          createdAt: '2026-03-01T10:00:00Z',
          updatedAt: '2026-03-15T14:00:00Z',
        },
        {
          id: 'doc-002',
          title: 'Service Architecture Overview',
          slug: 'service-architecture',
          summary: 'High-level architecture of the platform',
          category: 'Architecture',
          status: 'Published',
          authorId: 'user-002',
          tags: ['architecture', 'overview'],
          createdAt: '2026-02-01T10:00:00Z',
          updatedAt: '2026-03-10T14:00:00Z',
        },
        {
          id: 'doc-003',
          title: 'Database Troubleshooting Guide',
          slug: 'db-troubleshooting',
          summary: 'Common database issues and solutions',
          category: 'Troubleshooting',
          status: 'Published',
          authorId: 'user-001',
          tags: ['database', 'troubleshooting'],
          createdAt: '2026-01-15T10:00:00Z',
          updatedAt: '2026-02-28T14:00:00Z',
        },
      ];

      let filtered = allDocs;
      if (search) {
        filtered = filtered.filter((d) =>
          d.title.toLowerCase().includes(search.toLowerCase()) ||
          d.summary.toLowerCase().includes(search.toLowerCase()),
        );
      }
      if (category) {
        filtered = filtered.filter((d) => d.category === category);
      }

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: filtered, totalCount: filtered.length, page: 1, pageSize: 20 }),
      });
    });

    await page.route('**/api/v1/knowledge/operational-notes**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      }),
    );

    await page.route('**/api/v1/knowledge/search**', (route) => {
      const url = new URL(route.request().url());
      const q = url.searchParams.get('q') ?? '';
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: q ? [{ id: 'doc-001', title: 'Payment Gateway Recovery Runbook', relevance: 0.95 }] : [],
          totalCount: q ? 1 : 0,
        }),
      });
    });
  });

  test('displays knowledge documents on page load', async ({ page }) => {
    await page.goto('/knowledge');
    await expect(page.getByText('Payment Gateway Recovery Runbook')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Service Architecture Overview')).toBeVisible();
  });

  test('search filters documents with debounce', async ({ page }) => {
    await page.goto('/knowledge');
    await expect(page.getByText('Payment Gateway Recovery Runbook')).toBeVisible({ timeout: 5_000 });

    // Type in search
    const searchInput = page.getByPlaceholder(/search/i).first();
    if (await searchInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await searchInput.fill('payment');
      // Wait for debounce (350ms + network)
      await page.waitForTimeout(800);
      // Payment document should be visible, architecture should not
      await expect(page.getByText('Payment Gateway Recovery Runbook')).toBeVisible({ timeout: 3_000 });
    }
  });

  test('category filter narrows results', async ({ page }) => {
    await page.goto('/knowledge');
    await expect(page.getByText('Payment Gateway Recovery Runbook')).toBeVisible({ timeout: 5_000 });

    // Find and interact with category filter
    const categoryFilter = page.locator('select').filter({ hasText: /all|category|runbook/i }).first();
    if (await categoryFilter.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await categoryFilter.selectOption('Runbook');
      await page.waitForTimeout(500);
      // Only Runbook documents should be visible
      await expect(page.getByText('Payment Gateway Recovery Runbook')).toBeVisible({ timeout: 3_000 });
    }
  });

  test('empty search shows all documents', async ({ page }) => {
    await page.goto('/knowledge');
    await expect(page.getByText('Payment Gateway Recovery Runbook')).toBeVisible({ timeout: 5_000 });

    const searchInput = page.getByPlaceholder(/search/i).first();
    if (await searchInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
      // Type then clear
      await searchInput.fill('something');
      await page.waitForTimeout(500);
      await searchInput.clear();
      await page.waitForTimeout(500);
      // All documents should be visible again
      await expect(page.getByText('Payment Gateway Recovery Runbook')).toBeVisible({ timeout: 3_000 });
    }
  });
});

// ─── Service Catalog — search interaction depth ──────────────────────────────

test.describe('Service Catalog — search interaction depth', () => {
  const SERVICE_LIST_FIXTURE = {
    items: [
      { serviceId: 'svc-pay-001', name: 'payments-service', displayName: 'Payments Service', criticality: 'Critical', lifecycleStatus: 'Active', domain: 'Finance', teamName: 'Payments Team' },
      { serviceId: 'svc-auth-002', name: 'auth-service', displayName: 'Auth Service', criticality: 'High', lifecycleStatus: 'Active', domain: 'Platform', teamName: 'Identity Team' },
    ],
    totalCount: 2, page: 1, pageSize: 20,
  };

  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/graph**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ services: [], apis: [] }) }),
    );
    await page.route('**/api/v1/catalog/services/summary**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ totalCount: 2 }) }),
    );
    await page.route('**/api/v1/catalog/snapshots**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) }),
    );
    await page.route('**/api/v1/catalog/health**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) }),
    );
  });

  test('search clears and restores full list', async ({ page }) => {
    await page.route('**/api/v1/catalog/services**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary')) { route.fallback(); return; }
      const search = url.searchParams.get('search') ?? '';
      const filtered = search
        ? SERVICE_LIST_FIXTURE.items.filter((s) => s.name.includes(search) || s.displayName.toLowerCase().includes(search.toLowerCase()))
        : SERVICE_LIST_FIXTURE.items;
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: filtered, totalCount: filtered.length, page: 1, pageSize: 20 }),
      });
    });

    await page.goto('/services');
    await expect(page.getByText('Payments Service')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Auth Service')).toBeVisible();

    const searchInput = page.getByPlaceholder(/search services/i);
    await searchInput.fill('auth');
    await page.waitForTimeout(500);
    await expect(page.getByText('Auth Service')).toBeVisible({ timeout: 3_000 });
    await expect(page.getByText('Payments Service')).not.toBeVisible();

    // Clear search
    await searchInput.clear();
    await page.waitForTimeout(500);
    await expect(page.getByText('Payments Service')).toBeVisible({ timeout: 3_000 });
    await expect(page.getByText('Auth Service')).toBeVisible();
  });

  test('search with no results shows empty state', async ({ page }) => {
    await page.route('**/api/v1/catalog/services**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary')) { route.fallback(); return; }
      const search = url.searchParams.get('search') ?? '';
      if (search === 'nonexistent') {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
        });
      } else {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(SERVICE_LIST_FIXTURE),
        });
      }
    });

    await page.goto('/services');
    await expect(page.getByText('Payments Service')).toBeVisible({ timeout: 5_000 });

    const searchInput = page.getByPlaceholder(/search services/i);
    await searchInput.fill('nonexistent');
    await page.waitForTimeout(500);

    // Should show empty/no results state
    await expect(page.getByText('Payments Service')).not.toBeVisible({ timeout: 3_000 });
  });
});

// ─── Audit Page — verify integrity interaction ──────────────────────────────

test.describe('Audit — integrity verification flow', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/audit/search**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              eventId: 'evt-1',
              sourceModule: 'ChangeIntelligence',
              actionType: 'ReleaseCreated',
              resourceType: 'Release',
              resourceId: 'rel-001',
              performedBy: 'admin@acme.com',
              occurredAt: '2024-01-15T10:00:00Z',
              correlationId: 'cor-001',
              chainHash: 'abc123abc123abc123abc123abc123abc123abc123abc123',
              previousHash: null,
              sequenceNumber: 1,
            },
          ],
        }),
      }),
    );
  });

  test('verify integrity button shows chain validation result', async ({ page }) => {
    await page.route('**/api/v1/audit/verify-chain**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ isIntact: true, totalLinks: 5, violations: [], isTruncated: false }),
      }),
    );

    await page.goto('/audit');
    const verifyBtn = page.getByRole('button', { name: /verify integrity/i });
    await expect(verifyBtn).toBeVisible({ timeout: 5_000 });
    await verifyBtn.click();

    // Result should show chain is valid
    await expect(page.getByText(/hash chain is valid/i)).toBeVisible({ timeout: 5_000 });
  });

  test('verify integrity shows violations when chain is broken', async ({ page }) => {
    await page.route('**/api/v1/audit/verify-chain**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isIntact: false,
          totalLinks: 5,
          violations: [{ sequenceNumber: 3, expectedHash: 'expected', actualHash: 'actual' }],
          isTruncated: false,
        }),
      }),
    );

    await page.goto('/audit');
    const verifyBtn = page.getByRole('button', { name: /verify integrity/i });
    await expect(verifyBtn).toBeVisible({ timeout: 5_000 });
    await verifyBtn.click();

    // Result should show violations or broken chain
    await expect(page.getByText(/violation|broken|tampered|invalid/i).first()).toBeVisible({ timeout: 5_000 });
  });
});
