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
  | 'identity:users:read'
  | 'identity:users:write'
  | 'identity:roles:read'
  | 'identity:roles:assign'
  | 'identity:sessions:read'
  | 'identity:sessions:revoke'
  | 'identity:permissions:read'
  | 'engineering-graph:assets:read'
  | 'engineering-graph:assets:write'
  | 'contracts:read'
  | 'contracts:write'
  | 'contracts:import'
  | 'change-intelligence:releases:read'
  | 'change-intelligence:releases:write'
  | 'change-intelligence:blast-radius:read'
  | 'workflow:read'
  | 'workflow:write'
  | 'workflow:approve'
  | 'promotion:read'
  | 'promotion:write'
  | 'promotion:promote'
  | 'ruleset-governance:read'
  | 'ruleset-governance:write'
  | 'audit:read'
  | 'audit:export'
  | 'licensing:read'
  | 'licensing:write'
  | 'platform:settings:read'
  | 'platform:settings:write';
