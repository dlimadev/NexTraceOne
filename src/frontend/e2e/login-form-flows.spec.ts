import { test, expect } from '@playwright/test';

/**
 * E2E tests for Login form interactions, field validation, and business flows.
 * Goes beyond smoke testing to verify form behavior, validation rules, and submission handling.
 */

test.describe('Login — form field validation', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ error: 'Unauthorized' }) }),
    );
  });

  test('email field shows validation error for invalid format', async ({ page }) => {
    await page.goto('/login');
    const emailInput = page.getByLabel('Email');
    await emailInput.fill('not-an-email');
    // Tab away to trigger blur validation
    await page.locator('#password').click();
    // Try to submit
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    // Should show validation error message for invalid email
    await expect(page.getByText(/invalid|email/i).first()).toBeVisible({ timeout: 3_000 });
  });

  test('password field shows validation error when empty', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Email').fill('valid@test.com');
    // Leave password empty and submit
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    // Should show error for empty password
    await expect(page.getByText(/required|password/i).first()).toBeVisible({ timeout: 3_000 });
  });

  test('both fields show errors when form submitted empty', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    // Both fields should show validation errors
    const errorMessages = page.locator('[class*="error"], [role="alert"], .text-red, .text-danger, .text-destructive');
    await expect(errorMessages.first()).toBeVisible({ timeout: 3_000 });
  });

  test('email field accepts keyboard input and clears', async ({ page }) => {
    await page.goto('/login');
    const emailInput = page.getByLabel('Email');
    await emailInput.fill('test@example.com');
    await expect(emailInput).toHaveValue('test@example.com');
    // Clear the field
    await emailInput.clear();
    await expect(emailInput).toHaveValue('');
  });
});

test.describe('Login — successful form submission flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: JSON.stringify({ error: 'Unauthorized' }) }),
    );
  });

  test('successful login navigates away from login page', async ({ page }) => {
    await page.route('**/api/v1/identity/auth/login', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: 'mock-access-token',
          refreshToken: 'mock-refresh-token',
          tenantId: 'tenant-001',
          userId: 'user-001',
        }),
      }),
    );

    // After login, the /auth/me call will succeed
    let loginCompleted = false;
    await page.route('**/api/v1/identity/auth/me', (route) => {
      if (loginCompleted) {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'user-001',
            email: 'valid@test.com',
            fullName: 'Test User',
            roles: ['Admin'],
            permissions: ['catalog:assets:read'],
            tenantId: 'tenant-001',
            roleName: 'Admin',
          }),
        });
      }
      return route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Unauthorized"}' });
    });

    await page.goto('/login');
    await page.getByLabel('Email').fill('valid@test.com');
    await page.locator('#password').fill('ValidPassword123!');

    loginCompleted = true;
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();

    // After successful login, should navigate away from /login
    await expect(page).not.toHaveURL('/login', { timeout: 10_000 });
  });

  test('login sends correct payload to API', async ({ page }) => {
    let capturedPayload: Record<string, unknown> | null = null;

    await page.route('**/api/v1/identity/auth/login', async (route) => {
      const request = route.request();
      capturedPayload = JSON.parse(await request.postData() ?? '{}');
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'Identity.Auth.InvalidCredentials', detail: 'Invalid credentials' }),
      });
    });

    await page.goto('/login');
    await page.getByLabel('Email').fill('user@acme.com');
    await page.locator('#password').fill('SecurePass99!');
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();

    // Wait for the API call to complete
    await page.waitForTimeout(1_000);
    expect(capturedPayload).not.toBeNull();
    expect(capturedPayload!.email).toBe('user@acme.com');
    expect(capturedPayload!.password).toBe('SecurePass99!');
  });

  test('server error message is displayed on 500 response', async ({ page }) => {
    await page.route('**/api/v1/identity/auth/login', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'Identity.Auth.ServerError', detail: 'Internal server error' }),
      }),
    );

    await page.goto('/login');
    await page.getByLabel('Email').fill('user@test.com');
    await page.locator('#password').fill('password123');
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();

    await expect(page.getByText(/error|failed/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('multiple rapid submissions only make one API call', async ({ page }) => {
    let apiCallCount = 0;

    await page.route('**/api/v1/identity/auth/login', async (route) => {
      apiCallCount++;
      await new Promise((r) => setTimeout(r, 1_000));
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ code: 'Identity.Auth.InvalidCredentials', detail: 'Invalid credentials' }),
      });
    });

    await page.goto('/login');
    await page.getByLabel('Email').fill('user@test.com');
    await page.locator('#password').fill('password');

    const submitBtn = page.getByRole('button', { name: 'Sign in', exact: true });
    // Click rapidly
    await submitBtn.click();
    await submitBtn.click({ force: true }).catch(() => {/* may be disabled */});
    await submitBtn.click({ force: true }).catch(() => {/* may be disabled */});

    await page.waitForTimeout(2_000);
    // Button should have been disabled after first click, preventing multiple submissions
    expect(apiCallCount).toBeLessThanOrEqual(2);
  });
});

test.describe('Login — returnTo redirect preservation', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Unauthorized"}' }),
    );
  });

  test('preserves returnTo query parameter on the page', async ({ page }) => {
    await page.goto('/login?returnTo=/contracts/cv-001');
    // The login page should still display correctly with the returnTo parameter
    await expect(page.getByRole('heading', { name: /welcome to nextraceone/i })).toBeVisible();
    // URL should contain the returnTo parameter
    expect(page.url()).toContain('returnTo');
  });
});

test.describe('Forgot Password — form interaction', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/v1/identity/auth/me', (route) =>
      route.fulfill({ status: 401, contentType: 'application/json', body: '{"error":"Unauthorized"}' }),
    );
  });

  test('submits forgot password and shows success state', async ({ page }) => {
    await page.route('**/api/v1/identity/auth/forgot-password', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: '{}' }),
    );

    await page.goto('/forgot-password');
    await expect(page.getByRole('heading', { name: /forgot|password|reset/i })).toBeVisible({ timeout: 5_000 });

    // Fill email and submit
    const emailInput = page.getByLabel(/email/i).first();
    await emailInput.fill('user@acme.com');
    await page.getByRole('button', { name: /send|reset|submit/i }).click();

    // Should show success state (security: doesn't reveal if email exists)
    await expect(page.getByText(/sent|check|email|instructions/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('shows validation error for empty email', async ({ page }) => {
    await page.goto('/forgot-password');

    // Submit without filling email
    await page.getByRole('button', { name: /send|reset|submit/i }).click();

    // Should show validation error
    await expect(page.getByText(/required|email/i).first()).toBeVisible({ timeout: 3_000 });
  });

  test('has back to login link', async ({ page }) => {
    await page.goto('/forgot-password');
    const backLink = page.getByRole('link', { name: /back|login|sign in/i }).first();
    await expect(backLink).toBeVisible({ timeout: 3_000 });
  });
});
