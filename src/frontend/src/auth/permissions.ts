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
  | 'Admin'
  | 'Manager'
  | 'TechLead'
  | 'Developer'
  | 'Viewer'
  | 'Auditor'
  | 'SecurityReview'
  | 'ApprovalOnly';

// Permissões granulares por módulo — códigos idênticos ao RolePermissionCatalog do backend.
// Manter sincronizado com: IdentityAccess.Domain/Entities/RolePermissionCatalog.cs
export type Permission =
  // ── Identity & Access ──
  | 'identity:users:read'
  | 'identity:users:write'
  | 'identity:roles:read'
  | 'identity:roles:assign'
  | 'identity:sessions:read'
  | 'identity:sessions:revoke'
  | 'identity:permissions:read'
  | 'identity:jit-access:decide'
  | 'identity:break-glass:decide'
  | 'identity:delegations:manage'
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
  | 'change-intelligence:write'
  // ── Workflow ──
  | 'workflow:instances:read'
  | 'workflow:instances:write'
  | 'workflow:templates:write'
  // ── Promotion ──
  | 'promotion:requests:read'
  | 'promotion:requests:write'
  | 'promotion:environments:write'
  | 'promotion:gates:override'
  // ── Ruleset Governance ──
  | 'rulesets:read'
  | 'rulesets:write'
  | 'rulesets:execute'
  // ── Operations ──
  | 'operations:incidents:read'
  | 'operations:incidents:write'
  | 'operations:mitigation:read'
  | 'operations:mitigation:write'
  | 'operations:runbooks:read'
  | 'operations:runbooks:write'
  | 'operations:reliability:read'
  | 'operations:reliability:write'
  | 'operations:runtime:read'
  | 'operations:runtime:write'
  | 'operations:cost:read'
  | 'operations:cost:write'
  | 'operations:automation:read'
  | 'operations:automation:write'
  | 'operations:automation:execute'
  | 'operations:automation:approve'
  // ── AI Hub ──
  | 'ai:assistant:read'
  | 'ai:assistant:write'
  | 'ai:governance:read'
  | 'ai:governance:write'
  | 'ai:ide:read'
  | 'ai:ide:write'
  | 'ai:runtime:read'
  | 'ai:runtime:write'
  // ── Governance ──
  | 'governance:domains:read'
  | 'governance:domains:write'
  | 'governance:teams:read'
  | 'governance:teams:write'
  | 'governance:policies:read'
  | 'governance:controls:read'
  | 'governance:compliance:read'
  | 'governance:risk:read'
  | 'governance:evidence:read'
  | 'governance:waivers:read'
  | 'governance:waivers:write'
  | 'governance:packs:read'
  | 'governance:packs:write'
  | 'governance:reports:read'
  | 'governance:finops:read'
  | 'governance:admin:read'
  | 'governance:admin:write'
  // ── Product Analytics ──
  | 'analytics:read'
  | 'analytics:write'
  // ── Audit ──
  | 'audit:trail:read'
  | 'audit:reports:read'
  | 'audit:compliance:read'
  | 'audit:compliance:write'
  | 'audit:events:write'
  // ── Integrations ──
  | 'integrations:read'
  | 'integrations:write'
  // ── Configuration ──
  | 'configuration:read'
  | 'configuration:write'
  // ── Platform ──
  | 'platform:admin:read'
  | 'platform:settings:read'
  | 'platform:settings:write'
  // ── Notifications ──
  | 'notifications:inbox:read'
  | 'notifications:inbox:write'
  | 'notifications:preferences:read'
  | 'notifications:preferences:write'
  | 'notifications:configuration:read'
  | 'notifications:configuration:write'
  | 'notifications:delivery:read'
  // ── Environment Management ──
  | 'env:environments:read'
  | 'env:environments:write'
  | 'env:environments:admin'
  | 'env:access:read'
  | 'env:access:admin';
