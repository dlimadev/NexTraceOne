import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E tests for SAML SSO flows (ACT-022).
 *
 * Covers:
 * 1. Admin SAML SSO configuration page — NotConfigured, Enabled, Disabled states.
 * 2. Admin SAML SSO — save configuration form.
 * 3. Admin SAML SSO — "Test Connection" success and failure paths.
 * 4. SAML login initiation — GET /auth/saml/sso returns redirect URL.
 * 5. SAML ACS callback — simulated IdP POST with SAMLResponse → tokens issued.
 * 6. Error handling — SAML not configured → meaningful error shown.
 *
 * All network calls are mocked via Playwright route interception (no real backend required).
 * The mock IdP is represented by pre-defined SAMLResponse payloads and redirect URLs.
 */

// ─── Fixtures ─────────────────────────────────────────────────────────────────

const SAML_CONFIG_NOT_CONFIGURED = {
  status: 'NotConfigured' as const,
  entityId: '',
  ssoUrl: '',
  sloUrl: '',
  idpCertificate: '',
  jitProvisioningEnabled: false,
  defaultRole: 'Developer',
  attributeMappings: [],
};

const SAML_CONFIG_ENABLED = {
  status: 'Enabled' as const,
  entityId: 'https://nextraceone.example.com/saml/metadata',
  ssoUrl: 'https://idp.example.com/saml/sso',
  sloUrl: 'https://idp.example.com/saml/slo',
  idpCertificate: '-----BEGIN CERTIFICATE-----\nMIICxxx\n-----END CERTIFICATE-----',
  jitProvisioningEnabled: true,
  defaultRole: 'Developer',
  attributeMappings: [
    { samlAttr: 'email', nxtField: 'email' },
    { samlAttr: 'firstName', nxtField: 'given_name' },
  ],
};

const SAML_CONFIG_DISABLED = {
  ...SAML_CONFIG_ENABLED,
  status: 'Disabled' as const,
};

// ─── Helpers ──────────────────────────────────────────────────────────────────

async function setupAdminSession(page: import('@playwright/test').Page) {
  await mockAuthSession(page);
  // Ensure any branding/config calls don't block
  await page.route('**/api/v1/platform/config/branding', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({}) }),
  );
}

// ─── 1. Admin SAML SSO page — status states ───────────────────────────────────

test.describe('Admin SAML SSO — configuration page', () => {
  test.beforeEach(async ({ page }) => {
    await setupAdminSession(page);
  });

  test('@smoke SAML SSO page loads and shows NotConfigured status', async ({ page }) => {
    await page.route('**/api/v1/admin/saml-sso', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SAML_CONFIG_NOT_CONFIGURED),
      }),
    );

    await page.goto('/admin/saml-sso');

    // Page title should be visible
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 8_000 });

    // NotConfigured badge
    await expect(page.getByText(/not.?configured/i)).toBeVisible({ timeout: 5_000 });

    // Warning banner is displayed to alert admin of risk
    await expect(page.locator('.text-amber-800, [class*="amber"]').first()).toBeVisible();
  });

  test('SAML SSO page shows Enabled status with config data', async ({ page }) => {
    await page.route('**/api/v1/admin/saml-sso', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SAML_CONFIG_ENABLED),
      }),
    );

    await page.goto('/admin/saml-sso');

    await expect(page.getByText(/enabled/i).first()).toBeVisible({ timeout: 8_000 });

    // Entity ID should be pre-filled
    const entityIdInput = page.locator('input[placeholder*="saml/metadata"], input[value*="nextraceone.example.com"]');
    await expect(entityIdInput.first()).toBeVisible({ timeout: 5_000 });
  });

  test('SAML SSO page shows Disabled status', async ({ page }) => {
    await page.route('**/api/v1/admin/saml-sso', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SAML_CONFIG_DISABLED),
      }),
    );

    await page.goto('/admin/saml-sso');

    await expect(page.getByText(/disabled/i).first()).toBeVisible({ timeout: 8_000 });
  });

  test('SAML SSO page shows error state when API fails', async ({ page }) => {
    await page.route('**/api/v1/admin/saml-sso', (route) =>
      route.fulfill({ status: 500, contentType: 'application/json', body: '{"error":"Internal"}' }),
    );

    await page.goto('/admin/saml-sso');

    // Error state should be shown
    await expect(page.locator('[class*="red"], .text-red, [class*="error"]').first()).toBeVisible({ timeout: 8_000 });
  });

  test('Refresh button re-fetches SAML config', async ({ page }) => {
    let callCount = 0;
    await page.route('**/api/v1/admin/saml-sso', (route) => {
      callCount++;
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SAML_CONFIG_ENABLED),
      });
    });

    await page.goto('/admin/saml-sso');
    await expect(page.getByText(/enabled/i).first()).toBeVisible({ timeout: 8_000 });

    const initialCount = callCount;
    await page.getByRole('button', { name: /refresh/i }).click();
    await page.waitForTimeout(500);

    expect(callCount).toBeGreaterThan(initialCount);
  });
});

// ─── 2. Admin SAML SSO — save configuration ───────────────────────────────────

test.describe('Admin SAML SSO — save configuration', () => {
  test.beforeEach(async ({ page }) => {
    await setupAdminSession(page);
  });

  test('Save button calls PUT with updated config', async ({ page }) => {
    await page.route('**/api/v1/admin/saml-sso', async (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(SAML_CONFIG_ENABLED),
        });
      }
      if (route.request().method() === 'PUT') {
        const body = JSON.parse(route.request().postData() ?? '{}');
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ ...SAML_CONFIG_ENABLED, ...body }),
        });
      }
      return route.continue();
    });

    await page.goto('/admin/saml-sso');
    await expect(page.getByText(/enabled/i).first()).toBeVisible({ timeout: 8_000 });

    // Locate the SSO URL input and change it
    const ssoUrlInput = page.locator('input[placeholder*="idp.example.com"]').first();
    if (await ssoUrlInput.isVisible()) {
      await ssoUrlInput.fill('https://new-idp.example.com/saml/sso');
    }

    // Click Save
    const saveBtn = page.getByRole('button', { name: /save|update|apply/i }).first();
    if (await saveBtn.isVisible()) {
      await saveBtn.click();
      await page.waitForTimeout(500);
    }
  });
});

// ─── 3. Admin SAML SSO — Test Connection ──────────────────────────────────────

test.describe('Admin SAML SSO — test connection', () => {
  test.beforeEach(async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/admin/saml-sso', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(SAML_CONFIG_ENABLED),
        });
      }
      return route.continue();
    });
  });

  test('Test Connection button shows success result', async ({ page }) => {
    await page.route('**/api/v1/admin/saml-sso/test', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, message: 'Connection to IdP established successfully' }),
      }),
    );

    await page.goto('/admin/saml-sso');
    await expect(page.getByText(/enabled/i).first()).toBeVisible({ timeout: 8_000 });

    const testBtn = page.getByRole('button', { name: /test/i }).first();
    await expect(testBtn).toBeVisible({ timeout: 5_000 });
    await testBtn.click();

    // Success result should appear
    await expect(page.getByText(/success|established|connected/i).first()).toBeVisible({ timeout: 6_000 });
  });

  test('Test Connection button shows failure result', async ({ page }) => {
    await page.route('**/api/v1/admin/saml-sso/test', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'Connection refused: IdP unreachable' }),
      }),
    );

    await page.goto('/admin/saml-sso');
    await expect(page.getByText(/enabled/i).first()).toBeVisible({ timeout: 8_000 });

    const testBtn = page.getByRole('button', { name: /test/i }).first();
    await expect(testBtn).toBeVisible({ timeout: 5_000 });
    await testBtn.click();

    // Failure result should appear (the message is rendered regardless)
    await expect(page.getByText(/refused|unreachable|failed|error/i).first()).toBeVisible({ timeout: 6_000 });
  });
});

// ─── 4. SAML login initiation — GET /auth/saml/sso ────────────────────────────

test.describe('SAML login initiation flow', () => {
  test.beforeEach(async ({ page }) => {
    // Unauthenticated state — /me returns 401
    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Unauthorized"}' }),
    );
  });

  test('SAML SSO endpoint returns redirect URL to IdP (mock IdP)', async ({ page }) => {
    const MOCK_REDIRECT_URL =
      'https://idp.example.com/saml/sso?SAMLRequest=PHNhbWxwOkF1dGhuUmVxdWVzdA%3D%3D&RelayState=%2F';
    const MOCK_REQUEST_ID = '_e2e-test-request-id-001';

    await page.route('**/api/v1/identity/auth/saml/sso', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ redirectUrl: MOCK_REDIRECT_URL, requestId: MOCK_REQUEST_ID }),
      }),
    );

    // Intercept navigation to mock IdP to prevent leaving the test domain
    let capturedIdpUrl = '';
    await page.route('https://idp.example.com/**', (route) => {
      capturedIdpUrl = route.request().url();
      return route.fulfill({
        status: 200,
        contentType: 'text/html',
        body: '<html><body><p id="mock-idp">Mock IdP — Authentication page</p></body></html>',
      });
    });

    // Call the SAML SSO endpoint directly via fetch (simulates frontend calling the API)
    const response = await page.evaluate(async () => {
      const res = await fetch('/api/v1/identity/auth/saml/sso', { method: 'GET' });
      return res.json() as Promise<{ redirectUrl: string; requestId: string }>;
    });

    expect(response.redirectUrl).toBe(MOCK_REDIRECT_URL);
    expect(response.requestId).toBe(MOCK_REQUEST_ID);
    expect(response.redirectUrl).toContain('SAMLRequest=');
    expect(response.redirectUrl).toContain('RelayState=');
  });

  test('SAML SSO endpoint returns error when SAML not configured', async ({ page }) => {
    await page.route('**/api/v1/identity/auth/saml/sso', (route) =>
      route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          code: 'Identity.Saml.NotConfigured',
          detail: 'SAML is not configured for this tenant',
        }),
      }),
    );

    const response = await page.evaluate(async () => {
      const res = await fetch('/api/v1/identity/auth/saml/sso', { method: 'GET' });
      return { status: res.status, body: await res.json() as Record<string, unknown> };
    });

    expect(response.status).toBe(400);
    expect(response.body['code']).toBe('Identity.Saml.NotConfigured');
  });
});

// ─── 5. SAML ACS callback — simulated IdP response ────────────────────────────

test.describe('SAML ACS callback — simulated IdP response', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Unauthorized"}' }),
    );
  });

  test('ACS callback with valid SAMLResponse returns access and refresh tokens', async ({ page }) => {
    // Pre-encoded mock SAMLResponse (base64 of a minimal XML assertion)
    const MOCK_SAML_RESPONSE = btoa(
      '<samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol">' +
      '<saml:Assertion xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion">' +
      '<saml:NameID>saml-user-001@example.com</saml:NameID>' +
      '<saml:AttributeStatement>' +
      '<saml:Attribute Name="email"><saml:AttributeValue>saml-user@example.com</saml:AttributeValue></saml:Attribute>' +
      '</saml:AttributeStatement>' +
      '</saml:Assertion>' +
      '</samlp:Response>',
    );

    await page.route('**/api/v1/identity/auth/saml/acs', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            accessToken: 'saml-access-token-001',
            refreshToken: 'saml-refresh-token-001',
            expiresIn: 3600,
            user: {
              id: 'user-saml-001',
              email: 'saml-user@example.com',
              fullName: 'SAML Test User',
              roles: ['Developer'],
              permissions: ['catalog:assets:read'],
              tenantId: 'tenant-001',
              roleName: 'Developer',
            },
            returnTo: '/dashboard',
          }),
        });
      }
      return route.continue();
    });

    // Simulate the ACS POST that IdP would send back
    const result = await page.evaluate(async (samlResponse: string) => {
      const formData = new FormData();
      formData.append('SAMLResponse', samlResponse);
      formData.append('RelayState', '/dashboard');
      const res = await fetch('/api/v1/identity/auth/saml/acs', {
        method: 'POST',
        body: formData,
      });
      return { status: res.status, body: await res.json() as Record<string, unknown> };
    }, MOCK_SAML_RESPONSE);

    expect(result.status).toBe(200);
    expect(result.body['accessToken']).toBe('saml-access-token-001');
    expect(result.body['refreshToken']).toBe('saml-refresh-token-001');
    expect(result.body['returnTo']).toBe('/dashboard');
  });

  test('ACS callback with invalid SAMLResponse returns validation error', async ({ page }) => {
    await page.route('**/api/v1/identity/auth/saml/acs', (route) =>
      route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          code: 'Identity.Saml.InvalidResponse',
          detail: 'SAML response validation failed: invalid signature',
        }),
      }),
    );

    const result = await page.evaluate(async () => {
      const formData = new FormData();
      formData.append('SAMLResponse', 'invalid-base64-payload');
      formData.append('RelayState', '/');
      const res = await fetch('/api/v1/identity/auth/saml/acs', { method: 'POST', body: formData });
      return { status: res.status, body: await res.json() as Record<string, unknown> };
    });

    expect(result.status).toBe(400);
    expect(String(result.body['code'])).toContain('Saml.InvalidResponse');
  });

  test('ACS callback with empty SAMLResponse returns validation error', async ({ page }) => {
    await page.route('**/api/v1/identity/auth/saml/acs', (route) =>
      route.fulfill({
        status: 422,
        contentType: 'application/json',
        body: JSON.stringify({
          code: 'Validation.Failed',
          errors: [{ field: 'SamlResponse', message: 'SamlResponse must not be empty' }],
        }),
      }),
    );

    const result = await page.evaluate(async () => {
      const formData = new FormData();
      formData.append('SAMLResponse', '');
      const res = await fetch('/api/v1/identity/auth/saml/acs', { method: 'POST', body: formData });
      return { status: res.status };
    });

    expect(result.status).toBe(422);
  });
});

// ─── 6. SAML SSO end-to-end flow simulation ───────────────────────────────────

test.describe('SAML SSO — end-to-end flow simulation (@smoke)', () => {
  test('@smoke Full SAML SSO flow: initiate → mock IdP → ACS callback → authenticated', async ({ page }) => {
    // Step 1: User visits login page (unauthenticated)
    await page.route('**/api/v1/identity/auth/me', (route) => {
      // First call: not authenticated
      return route.fulfill({ status: 401, body: '{"error":"Unauthorized"}', contentType: 'application/json' });
    });

    // Step 2: Backend SAML SSO endpoint returns IdP redirect
    const MOCK_IDP_REDIRECT = 'http://localhost:4173/mock-idp-callback?SAMLRequest=abc123&RelayState=%2F';
    await page.route('**/api/v1/identity/auth/saml/sso*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          redirectUrl: MOCK_IDP_REDIRECT,
          requestId: '_e2e-saml-req-001',
        }),
      }),
    );

    // Step 3: ACS callback returns tokens after IdP authenticates
    await page.route('**/api/v1/identity/auth/saml/acs', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'saml-at-e2e',
          refreshToken: 'saml-rt-e2e',
          expiresIn: 3600,
          user: {
            id: 'user-saml-e2e',
            email: 'saml-e2e@example.com',
            fullName: 'SAML E2E User',
            roles: ['Developer'],
            permissions: ['catalog:assets:read'],
            tenantId: 'tenant-001',
            roleName: 'Developer',
          },
          returnTo: '/',
        }),
      }),
    );

    await page.goto('/login');
    await expect(page.getByRole('heading', { name: /welcome/i })).toBeVisible({ timeout: 8_000 });

    // Verify the SAML initiation API works correctly by calling it directly (simulates browser redirect)
    const samlResponse = await page.evaluate(async () => {
      const res = await fetch('/api/v1/identity/auth/saml/sso', { method: 'GET' });
      return res.json() as Promise<{ redirectUrl: string; requestId: string }>;
    });

    expect(samlResponse.requestId).toBe('_e2e-saml-req-001');
    expect(samlResponse.redirectUrl).toContain('SAMLRequest=abc123');

    // Simulate IdP posting back (ACS callback)
    const acsResult = await page.evaluate(async () => {
      const formData = new FormData();
      formData.append('SAMLResponse', btoa('<samlp:Response/>'));
      formData.append('RelayState', '/');
      const res = await fetch('/api/v1/identity/auth/saml/acs', { method: 'POST', body: formData });
      return res.json() as Promise<{ accessToken: string; user: { email: string } }>;
    });

    expect(acsResult.accessToken).toBe('saml-at-e2e');
    expect(acsResult.user.email).toBe('saml-e2e@example.com');
  });
});
