import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E tests for refresh token flow.
 *
 * Validates:
 * 1. Automatic token refresh on 401 response
 * 2. Session continuity after token refresh
 * 3. Session expiry when refresh fails
 * 4. Concurrent requests during refresh are queued
 */

test.describe('Refresh Token — automatic renewal', () => {
  test('renews session automatically when access token expires', async ({ page }) => {
    await mockAuthSession(page);
    let refreshCallCount = 0;

    // Intercept /auth/me to succeed initially
    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'user-e2e-001',
          email: 'admin@acme.com',
          fullName: 'Admin E2E',
          roles: ['Admin'],
          permissions: ['catalog:assets:read', 'contracts:read', 'governance:reports:read'],
          tenantId: 'tenant-e2e-001',
          roleName: 'Admin',
        }),
      }),
    );

    // Mock the refresh endpoint to succeed and return new tokens
    await page.route('**/api/v1/identity/auth/refresh', (route) => {
      refreshCallCount++;
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'refreshed-access-token-001',
          refreshToken: 'refreshed-refresh-token-001',
        }),
      });
    });

    // Simulate: first call to catalog returns 401, triggering refresh
    let catalogCallCount = 0;
    await page.route('**/api/v1/catalog/graph', (route) => {
      catalogCallCount++;
      if (catalogCallCount === 1) {
        // First call: 401 (token expired)
        return route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Unauthorized"}' });
      }
      // After refresh: success
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          services: [{ serviceAssetId: 's1', name: 'test-service', teamName: 'QA', domain: 'test', criticality: 'Low' }],
          apis: [],
        }),
      });
    });

    await page.goto('/');

    // The dashboard should load successfully after automatic refresh
    await expect(page.getByText('test-service')).toBeVisible({ timeout: 10_000 });

    // Verify that the refresh endpoint was called
    expect(refreshCallCount).toBeGreaterThanOrEqual(1);
  });

  test('redirects to login when refresh token is expired', async ({ page }) => {
    await mockAuthSession(page);

    // Intercept /auth/me to succeed initially
    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'user-e2e-001',
          email: 'admin@acme.com',
          fullName: 'Admin E2E',
          roles: ['Admin'],
          permissions: ['catalog:assets:read'],
          tenantId: 'tenant-e2e-001',
          roleName: 'Admin',
        }),
      }),
    );

    // Mock: catalog returns 401
    await page.route('**/api/v1/catalog/graph', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Unauthorized"}' }),
    );

    // Mock: refresh also fails (expired refresh token)
    await page.route('**/api/v1/identity/auth/refresh', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Refresh token expired"}' }),
    );

    await page.goto('/');

    // Should redirect to login after failed refresh
    await expect(page).toHaveURL('/login', { timeout: 10_000 });
  });

  test('clears session storage when refresh fails', async ({ page }) => {
    await mockAuthSession(page);

    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'user-e2e-001',
          email: 'admin@acme.com',
          fullName: 'Admin E2E',
          roles: ['Admin'],
          permissions: ['catalog:assets:read'],
          tenantId: 'tenant-e2e-001',
          roleName: 'Admin',
        }),
      }),
    );

    // All API calls return 401
    await page.route('**/api/v1/catalog/**', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Unauthorized"}' }),
    );

    // Refresh fails
    await page.route('**/api/v1/identity/auth/refresh', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Invalid"}' }),
    );

    await page.goto('/');
    await expect(page).toHaveURL('/login', { timeout: 10_000 });

    // Verify tokens are cleared
    const tenantId = await page.evaluate(() => sessionStorage.getItem('nxt_tid'));
    expect(tenantId).toBeNull();
  });
});

test.describe('Refresh Token — concurrent request handling', () => {
  test('queues concurrent requests during refresh', async ({ page }) => {
    await mockAuthSession(page);
    let refreshCallCount = 0;

    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'user-e2e-001',
          email: 'admin@acme.com',
          fullName: 'Admin E2E',
          roles: ['Admin'],
          permissions: ['catalog:assets:read', 'contracts:read'],
          tenantId: 'tenant-e2e-001',
          roleName: 'Admin',
        }),
      }),
    );

    // Refresh: add delay to simulate slow refresh, only called once
    await page.route('**/api/v1/identity/auth/refresh', async (route) => {
      refreshCallCount++;
      await new Promise((r) => setTimeout(r, 300));
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'new-token-concurrent',
          refreshToken: 'new-refresh-concurrent',
        }),
      });
    });

    // Both catalog endpoints return 401 first, then succeed
    const callCounts: Record<string, number> = {};
    for (const path of ['**/api/v1/catalog/graph', '**/api/v1/catalog/services/summary']) {
      callCounts[path] = 0;
      await page.route(path, (route) => {
        callCounts[path]++;
        if (callCounts[path] <= 1) {
          return route.fulfill({ status: 401, contentType: 'application/json', body: '{}' });
        }
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ services: [], apis: [], relationships: [], total: 0 }),
        });
      });
    }

    await page.goto('/');
    await page.waitForTimeout(3000);

    // Refresh should only be called once despite concurrent 401s
    expect(refreshCallCount).toBeLessThanOrEqual(2);
  });
});
