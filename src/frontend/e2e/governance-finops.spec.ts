import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E tests for Governance and FinOps browser journeys.
 * Covers GAP-018: browser-based E2E for governance surfaces.
 */

const GOVERNANCE_HEALTH_FIXTURE = {
  overallStatus: 'Healthy',
  database: { status: 'Healthy', latencyMs: 12 },
  backgroundJobs: { status: 'Unknown', latencyMs: 0 },
  ingestion: { status: 'Unknown', latencyMs: 0 },
  ai: { status: 'Healthy', latencyMs: 45 },
};

const FINOPS_SUMMARY_FIXTURE = {
  totalMonthlyCost: 12500.0,
  costTrend: 'Stable',
  topServices: [
    { serviceName: 'payments-service', monthlyCost: 4200.0 },
    { serviceName: 'auth-service', monthlyCost: 2800.0 },
  ],
  wasteSignals: 3,
  efficiencyScore: 78,
  isSimulated: false,
};

const COMPLIANCE_FIXTURE = {
  items: [
    { id: 'cc-001', name: 'API versioning compliance', status: 'Passed', severity: 'High' },
    { id: 'cc-002', name: 'Contract ownership required', status: 'Passed', severity: 'Medium' },
    { id: 'cc-003', name: 'Breaking change detection', status: 'Failed', severity: 'Critical' },
  ],
  totalCount: 3,
};

const RISK_FIXTURE = {
  items: [
    { id: 'r-001', title: 'Unowned services detected', severity: 'High', status: 'Open', affectedServices: 2 },
    { id: 'r-002', title: 'Deprecated APIs still in use', severity: 'Medium', status: 'Open', affectedServices: 1 },
  ],
  totalCount: 2,
};

// ─── Governance Reports ───────────────────────────────────────────────────────

test.describe('Governance — Reports page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/governance/executive-drill-down**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalServices: 15,
          totalContracts: 42,
          complianceScore: 87,
          riskLevel: 'Medium',
          isSimulated: false,
        }),
      }),
    );
  });

  test('opens governance reports page', async ({ page }) => {
    await page.goto('/governance/reports');
    await expect(page.getByRole('heading', { name: /report/i })).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Governance FinOps ────────────────────────────────────────────────────────

test.describe('Governance — FinOps page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/governance/finops-summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(FINOPS_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/governance/waste-signals**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0 }),
      }),
    );
    await page.route('**/api/v1/governance/efficiency-indicators**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0 }),
      }),
    );
  });

  test('opens FinOps page and displays summary', async ({ page }) => {
    await page.goto('/governance/finops');
    await expect(page.getByRole('heading', { name: /finops/i })).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Governance Compliance ────────────────────────────────────────────────────

test.describe('Governance — Compliance page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/governance/compliance-checks**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(COMPLIANCE_FIXTURE),
      }),
    );
  });

  test('opens compliance page', async ({ page }) => {
    await page.goto('/governance/compliance');
    await expect(page.getByRole('heading', { name: /compliance/i })).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Governance Risk Center ───────────────────────────────────────────────────

test.describe('Governance — Risk Center page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/governance/risk-signals**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(RISK_FIXTURE),
      }),
    );
  });

  test('opens risk center page', async ({ page }) => {
    await page.goto('/governance/risk');
    await expect(page.getByRole('heading', { name: /risk/i })).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Platform Health ──────────────────────────────────────────────────────────

test.describe('Governance — Platform Health', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/governance/platform-health**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(GOVERNANCE_HEALTH_FIXTURE),
      }),
    );
  });

  test('opens platform ops and shows health status', async ({ page }) => {
    await page.goto('/operations/platform-ops');
    await expect(page.getByRole('heading', { name: /platform/i })).toBeVisible({ timeout: 5_000 });
  });
});
