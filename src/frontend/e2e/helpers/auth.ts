import type { Page } from '@playwright/test';

/**
 * Perfil de utilizador E2E completo retornado pela API de identidade.
 *
 * Segurança: utiliza sessionStorage com as chaves reais do tokenStorage (nxt_at, nxt_tid, nxt_uid).
 * O refresh token NÃO é persistido no storage (apenas em memória), conforme a estratégia de segurança.
 * O endpoint /api/v1/identity/auth/me é interceptado para devolver um perfil com permissões
 * completas, eliminando dependência de backend nos testes E2E.
 */

/** Permissões completas de um utilizador administrador para testes E2E. */
const ADMIN_PERMISSIONS = [
  'catalog:assets:read',
  'catalog:assets:write',
  'contracts:read',
  'contracts:write',
  'contracts:import',
  'developer-portal:read',
  'developer-portal:write',
  'change-intelligence:read',
  'change-intelligence:releases:read',
  'change-intelligence:releases:write',
  'change-intelligence:blast-radius:read',
  'workflow:read',
  'workflow:write',
  'workflow:approve',
  'promotion:read',
  'promotion:write',
  'promotion:promote',
  'operations:incidents:read',
  'operations:incidents:write',
  'operations:runbooks:read',
  'operations:runbooks:write',
  'operations:reliability:read',
  'operations:automation:read',
  'ai:assistant:read',
  'ai:models:read',
  'ai:policies:read',
  'ai:governance:read',
  'ai:ide:read',
  'identity:users:read',
  'identity:users:write',
  'identity:roles:read',
  'identity:sessions:read',
  'audit:read',
  'audit:export',
  'governance:packs:read',
  'governance:waivers:read',
  'governance:reports:read',
  'governance:risk:read',
  'governance:compliance:read',
  'governance:policies:read',
  'governance:evidence:read',
  'governance:controls:read',
  'governance:finops:read',
  'governance:analytics:read',
  'governance:domains:read',
  'governance:teams:read',
  'ruleset-governance:read',
  'ruleset-governance:write',
  'integrations:read',
  'platform:admin:read',
];

/** Permissões para o perfil Engineer (acesso limitado). */
const ENGINEER_PERMISSIONS = [
  'catalog:assets:read',
  'contracts:read',
  'contracts:write',
  'change-intelligence:read',
  'change-intelligence:releases:read',
  'operations:incidents:read',
  'operations:runbooks:read',
];

/**
 * Configura uma sessão autenticada completa para testes E2E.
 *
 * Intercepta dois endpoints:
 * 1. /api/v1/identity/auth/me — usado pelo AuthContext na inicialização
 * 2. /api/v1/identity/users/:uid — usado por getUserProfile
 *
 * Ambos devolvem o mesmo perfil com permissões configuráveis.
 */
export async function mockAuthSession(
  page: Page,
  options: {
    roles?: string[];
    permissions?: string[];
  } = {},
): Promise<void> {
  const roles = options.roles ?? ['Admin'];
  const permissions = options.permissions ?? ADMIN_PERMISSIONS;

  const profile = {
    id: 'user-e2e-001',
    email: 'admin@acme.com',
    fullName: 'Admin E2E',
    firstName: 'Admin',
    lastName: 'E2E',
    isActive: true,
    lastLoginAt: null,
    tenantId: 'tenant-e2e-001',
    roleName: roles[0] ?? 'Admin',
    roles,
    permissions,
  };

  // Tokens persistidos em sessionStorage (access + tenantId + userId)
  await page.addInitScript(() => {
    sessionStorage.setItem('nxt_at', 'mock-e2e-token');
    sessionStorage.setItem('nxt_tid', 'tenant-e2e-001');
    sessionStorage.setItem('nxt_uid', 'user-e2e-001');
  });

  // Endpoint usado pelo AuthContext.getCurrentUser() na inicialização
  await page.route('**/api/v1/identity/auth/me', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(profile),
    }),
  );

  // Endpoint usado por identityApi.getUserProfile(id)
  await page.route('**/api/v1/identity/users/user-e2e-001', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(profile),
    }),
  );
}

/**
 * Configura uma sessão para um utilizador Engineer (permissões reduzidas).
 */
export async function mockEngineerSession(page: Page): Promise<void> {
  return mockAuthSession(page, {
    roles: ['Developer'],
    permissions: ENGINEER_PERMISSIONS,
  });
}
