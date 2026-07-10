/**
 * Fixtures de autenticação para o modo stub (npm run stub).
 *
 * Estes dados alimentam os handlers MSW que substituem o backend real.
 * O objetivo é permitir arrancar a app autenticada, sem depender da API.
 *
 * @see src/stubs/handlers/auth.ts
 */
import type {
  CurrentUserProfile,
  LoginResponse,
  CookieSessionLoginResponse,
  TenantInfo,
} from '../../types';
import type { Permission } from '../../auth/permissions';
import type { EnvironmentItem, PersonaConfigResponse } from '../../features/identity-access/api/identity';

/** ID de tenant fixo usado em todo o modo stub. */
export const STUB_TENANT_ID = '00000000-0000-0000-0000-0000000000t1';

/** ID de utilizador fixo usado em todo o modo stub. */
export const STUB_USER_ID = '00000000-0000-0000-0000-0000000000u1';

/**
 * Conjunto completo de permissões — o utilizador stub é um PlatformAdmin
 * com acesso a tudo, para que nenhum elemento de UI fique escondido.
 * Mantém sincronizado com o tipo Permission em src/auth/permissions.ts.
 */
export const ALL_PERMISSIONS: Permission[] = [
  'identity:users:read', 'identity:users:write', 'identity:roles:read', 'identity:roles:assign',
  'identity:sessions:read', 'identity:sessions:revoke', 'identity:permissions:read',
  'identity:jit-access:decide', 'identity:break-glass:decide', 'identity:delegations:manage',
  'catalog:assets:read', 'catalog:assets:write',
  'catalog:templates:read', 'catalog:templates:write', 'catalog:templates:scaffold',
  'contracts:read', 'contracts:write', 'contracts:import',
  'developer-portal:read', 'developer-portal:write',
  'change-intelligence:read', 'change-intelligence:write',
  'workflow:instances:read', 'workflow:instances:write', 'workflow:templates:write',
  'promotion:requests:read', 'promotion:requests:write', 'promotion:environments:write', 'promotion:gates:override',
  'rulesets:read', 'rulesets:write', 'rulesets:execute',
  'operations:incidents:read', 'operations:incidents:write', 'operations:mitigation:read', 'operations:mitigation:write',
  'operations:runbooks:read', 'operations:runbooks:write', 'operations:reliability:read', 'operations:reliability:write',
  'operations:runtime:read', 'operations:runtime:write', 'operations:cost:read', 'operations:cost:write',
  'operations:automation:read', 'operations:automation:write', 'operations:automation:execute', 'operations:automation:approve',
  'ai:assistant:read', 'ai:assistant:write', 'ai:governance:read', 'ai:governance:write',
  'ai:ide:read', 'ai:ide:write', 'ai:runtime:read', 'ai:runtime:write',
  'governance:domains:read', 'governance:domains:write', 'governance:teams:read', 'governance:teams:write',
  'governance:policies:read', 'governance:controls:read', 'governance:compliance:read', 'governance:risk:read',
  'governance:evidence:read', 'governance:waivers:read', 'governance:waivers:write', 'governance:packs:read',
  'governance:packs:write', 'governance:reports:read', 'governance:finops:read', 'governance:admin:read',
  'governance:admin:write', 'governance:security:scan',
  'catalog:contracts:pipeline:read',
  'analytics:read', 'analytics:write', 'analytics:configure',
  'audit:trail:read', 'audit:reports:read', 'audit:compliance:read', 'audit:compliance:write', 'audit:events:write',
  'integrations:read', 'integrations:write', 'integrations:connectors:read',
  'configuration:read', 'configuration:write', 'configuration:admin', 'configuration:analytics:read',
  'platform:admin:read', 'platform:admin:write', 'platform:settings:read', 'platform:settings:write',
  'notifications:inbox:read', 'notifications:inbox:write', 'notifications:preferences:read',
  'notifications:preferences:write', 'notifications:configuration:read', 'notifications:configuration:write',
  'notifications:delivery:read', 'notifications:admin:read', 'notifications:admin:write',
  'env:environments:read', 'env:environments:write', 'env:environments:admin',
  'env:access:read', 'env:access:admin',
  'identity:tenants:admin',
  'operations:telemetry:read',
  'governance:gates:read', 'governance:reports:write',
];

/** Perfil devolvido por GET /identity/auth/me — faz a app arrancar autenticada. */
export const stubCurrentUser: CurrentUserProfile = {
  id: STUB_USER_ID,
  email: 'stub.admin@nextraceone.dev',
  firstName: 'Stub',
  lastName: 'Admin',
  fullName: 'Stub Admin',
  isActive: true,
  lastLoginAt: new Date().toISOString(),
  tenantId: STUB_TENANT_ID,
  tenantName: 'Stub Tenant',
  roleName: 'PlatformAdmin',
  permissions: ALL_PERMISSIONS,
  persona: 'platform-admin',
  createdAt: '2026-01-01T00:00:00.000Z',
  mustChangePassword: false,
  lastPasswordChangeAt: '2026-01-01T00:00:00.000Z',
};

/** Resposta do login por cookie de sessão. */
export const stubCookieSessionLogin: CookieSessionLoginResponse = {
  csrfToken: 'stub-csrf-token',
  expiresIn: 3600,
  user: {
    id: STUB_USER_ID,
    email: stubCurrentUser.email,
    fullName: stubCurrentUser.fullName,
    tenantId: STUB_TENANT_ID,
    roleName: stubCurrentUser.roleName,
    permissions: ALL_PERMISSIONS,
  },
};

/** Resposta do login por bearer token (fallback). */
export const stubBearerLogin: LoginResponse = {
  accessToken: 'stub-access-token',
  refreshToken: 'stub-refresh-token',
  expiresIn: 3600,
  user: stubCookieSessionLogin.user,
};

/** Tenant único — evita o ecrã de seleção de tenant. */
export const stubTenants: TenantInfo[] = [
  {
    id: STUB_TENANT_ID,
    name: 'Stub Tenant',
    slug: 'stub-tenant',
    isActive: true,
    roleName: 'PlatformAdmin',
  },
];

/** Ambientes do tenant stub. */
export const stubEnvironments: EnvironmentItem[] = [
  {
    id: '00000000-0000-0000-0000-0000000000e1',
    name: 'Production',
    slug: 'production',
    sortOrder: 0,
    isActive: true,
    profile: 'Production',
    criticality: 'Critical',
    isProductionLike: true,
    isPrimaryProduction: true,
    code: 'prod',
    region: 'eu-west-1',
  },
  {
    id: '00000000-0000-0000-0000-0000000000e2',
    name: 'Staging',
    slug: 'staging',
    sortOrder: 1,
    isActive: true,
    profile: 'Staging',
    criticality: 'Medium',
    isProductionLike: true,
    isPrimaryProduction: false,
    code: 'stg',
    region: 'eu-west-1',
  },
];

/** Configuração de persona devolvida por GET /identity/me/persona-config. */
export const stubPersonaConfig: PersonaConfigResponse = {
  persona: 'platform-admin',
  quickActions: [],
  prioritizedModules: [],
};
