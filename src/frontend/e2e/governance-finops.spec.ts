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
  totalWaste: 2100.0,
  overallEfficiency: 'Efficient',
  costTrend: 'Stable',
  services: [
    {
      serviceId: 'svc-pay-001',
      serviceName: 'payments-service',
      domain: 'Finance',
      team: 'Payments Team',
      monthlyCost: 4200.0,
      waste: 300.0,
      efficiency: 'Efficient',
      trend: 'Stable',
      wasteSignals: [],
    },
    {
      serviceId: 'svc-auth-001',
      serviceName: 'auth-service',
      domain: 'Platform',
      team: 'Identity Team',
      monthlyCost: 2800.0,
      waste: 100.0,
      efficiency: 'Acceptable',
      trend: 'Improving',
      wasteSignals: [],
    },
  ],
  topCostDrivers: [],
  topWasteSignals: [],
  optimizationOpportunities: [],
  generatedAt: '2026-03-18T14:00:00Z',
};

const COMPLIANCE_FIXTURE = {
  overallScore: 87,
  totalPacksAssessed: 3,
  compliantCount: 2,
  partiallyCompliantCount: 0,
  nonCompliantCount: 1,
  totalRollouts: 2,
  completedRollouts: 1,
  failedRollouts: 0,
  totalWaivers: 0,
  approvedWaivers: 0,
  packs: [
    { packId: 'cc-001', packName: 'API versioning compliance', complianceLevel: 'Compliant', assessedAt: '2026-03-01T10:00:00Z' },
    { packId: 'cc-002', packName: 'Contract ownership required', complianceLevel: 'Compliant', assessedAt: '2026-03-01T10:00:00Z' },
    { packId: 'cc-003', packName: 'Breaking change detection', complianceLevel: 'NonCompliant', assessedAt: '2026-03-01T10:00:00Z' },
  ],
};

const RISK_FIXTURE = {
  totalPacksAssessed: 2,
  criticalCount: 0,
  highCount: 1,
  mediumCount: 1,
  lowCount: 0,
  indicators: [
    { packId: 'r-001', packName: 'Unowned services detected', category: 'Ownership', riskLevel: 'High', dimensions: [] },
    { packId: 'r-002', packName: 'Deprecated APIs still in use', category: 'Lifecycle', riskLevel: 'Medium', dimensions: [] },
  ],
};

// ─── Governance Reports ───────────────────────────────────────────────────────

test.describe('Governance — Reports page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/reports/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalServices: 15,
          totalContracts: 42,
          complianceScore: 87,
          riskLevel: 'Medium',
          isSimulated: false,
          totalPacks: 5,
          coveredServices: 12,
          uncoveredServices: 3,
          packCoverage: 80,
        }),
      }),
    );
  });

  test('opens governance reports page', async ({ page }) => {
    await page.goto('/governance/reports');
    await expect(page.getByRole('heading', { name: /report/i, level: 1 })).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Governance FinOps ────────────────────────────────────────────────────────

test.describe('Governance — FinOps page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/finops/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(FINOPS_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/finops/trends**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0 }),
      }),
    );
  });

  test('opens FinOps page and displays summary', async ({ page }) => {
    await page.goto('/governance/finops');
    await expect(page.getByRole('heading', { name: /finops/i, level: 1 })).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Governance Compliance ────────────────────────────────────────────────────

test.describe('Governance — Compliance page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/compliance/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(COMPLIANCE_FIXTURE),
      }),
    );
  });

  test('opens compliance page', async ({ page }) => {
    await page.goto('/governance/compliance');
    await expect(page.getByRole('heading', { name: /compliance/i, level: 1 })).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Governance Risk Center ───────────────────────────────────────────────────

test.describe('Governance — Risk Center page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/risk/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(RISK_FIXTURE),
      }),
    );
  });

  test('opens risk center page', async ({ page }) => {
    await page.goto('/governance/risk');
    await expect(page.getByRole('heading', { name: /risk/i, level: 1 })).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Platform Health ──────────────────────────────────────────────────────────

test.describe('Governance — Platform Health', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/platform/health**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(GOVERNANCE_HEALTH_FIXTURE),
      }),
    );
  });

  test('opens platform ops and shows health status', async ({ page }) => {
    await page.goto('/platform/operations');
    await expect(page.getByRole('heading', { name: /platform/i, level: 1 })).toBeVisible({ timeout: 5_000 });
  });
});
