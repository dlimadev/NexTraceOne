/**
 * Catálogo de roles e permissões alinhado ao backend.
 *
 * IMPORTANTE — Segurança:
 * O frontend NUNCA deve fazer enforcement de autorização.
 * O backend é a única fonte de verdade para permissões.
 * O frontend usa as permissões recebidas do backend (via JWT/profile)
 * apenas para controle visual (exibir/ocultar elementos de UI).
 *
 * Não existe mapeamento client-side de role→permissões propositalmente.
 * As permissões efetivas do usuário são obtidas via endpoint /me
 * e refletidas pelo hook usePermissions.
 *
 * @see src/hooks/usePermissions.ts — hook que expõe permissões do usuário
 */

// Roles disponíveis no sistema RBAC do NexTraceOne (alinhados com o backend)
export type AppRole =
  | 'PlatformAdmin'
  | 'TechLead'
  | 'Developer'
  | 'Viewer'
  | 'Auditor'
  | 'SecurityReview'
  | 'ApprovalOnly';

// Permissões granulares por módulo (códigos idênticos ao catálogo do backend)
export type Permission =
  // ── Identity & Access ──
  | 'identity:users:read'
  | 'identity:users:write'
  | 'identity:roles:read'
  | 'identity:roles:assign'
  | 'identity:sessions:read'
  | 'identity:sessions:revoke'
  | 'identity:permissions:read'
  // ── Engineering Graph / Service Catalog ──
  | 'catalog:assets:read'
  | 'catalog:assets:write'
  // ── Contracts ──
  | 'contracts:read'
  | 'contracts:write'
  | 'contracts:import'
  // ── Developer Portal ──
  | 'developer-portal:read'
  | 'developer-portal:write'
  // ── Change Intelligence ──
  | 'change-intelligence:read'
  | 'change-intelligence:releases:read'
  | 'change-intelligence:releases:write'
  | 'change-intelligence:blast-radius:read'
  // ── Workflow ──
  | 'workflow:read'
  | 'workflow:write'
  | 'workflow:approve'
  // ── Promotion ──
  | 'promotion:read'
  | 'promotion:write'
  | 'promotion:promote'
  // ── Ruleset Governance ──
  | 'ruleset-governance:read'
  | 'ruleset-governance:write'
  // ── Operations ──
  | 'operations:incidents:read'
  | 'operations:incidents:write'
  | 'operations:runbooks:read'
  | 'operations:runbooks:write'
  // ── AI Hub ──
  | 'ai:assistant:read'
  | 'ai:models:read'
  | 'ai:models:write'
  | 'ai:policies:read'
  | 'ai:policies:write'
  // ── Governance ──
  | 'governance:reports:read'
  | 'governance:risk:read'
  | 'governance:compliance:read'
  | 'governance:finops:read'
  // ── Audit ──
  | 'audit:read'
  | 'audit:export'
  // ── Licensing ──
  | 'licensing:read'
  | 'licensing:write'
  | 'licensing:vendor:license:read'
  | 'licensing:vendor:license:create'
  | 'licensing:vendor:license:revoke'
  | 'licensing:vendor:license:rehost'
  | 'licensing:vendor:license:manage'
  | 'licensing:vendor:key:generate'
  | 'licensing:vendor:trial:extend'
  | 'licensing:vendor:activation:issue'
  | 'licensing:vendor:tenant:manage'
  | 'licensing:vendor:telemetry:view'
  | 'licensing:vendor:plan:read'
  | 'licensing:vendor:plan:create'
  | 'licensing:vendor:featurepack:read'
  | 'licensing:vendor:featurepack:create'
  // ── Platform ──
  | 'platform:settings:read'
  | 'platform:settings:write';
