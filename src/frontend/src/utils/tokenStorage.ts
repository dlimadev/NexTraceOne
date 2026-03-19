/**
 * Módulo de armazenamento seguro de tokens de autenticação.
 *
 * Estratégia de segurança:
 * - O refresh token é mantido EXCLUSIVAMENTE em memória (closure),
 *   jamais persistido em localStorage ou sessionStorage, para mitigar
 *   risco de exfiltração via XSS.
 * - O access token é armazenado em sessionStorage (escopo de aba),
 *   que é automaticamente limpo ao fechar a aba do navegador e não é
 *   compartilhado entre abas — reduzindo a superfície de ataque em
 *   comparação com localStorage.
 * - O tenant_id e user_id são mantidos em sessionStorage apenas para
 *   permitir re-hidratação após refresh da página.
 *
 * Riscos residuais documentados:
 * - sessionStorage ainda é acessível a código JS na mesma origem.
 *   A mitigação definitiva é migrar para httpOnly cookies com flags
 *   Secure e SameSite=Strict (requer alteração no backend).
 * - Em caso de XSS na mesma aba, o access_token pode ser lido.
 *   Isso é mitigado pela curta duração do access token (60 min)
 *   e pela ausência do refresh token no storage do browser.
 *
 * @see docs/security/application-security-review.md para detalhes completos.
 */

const SESSION_KEYS = {
  TENANT_ID: 'nxt_tid',
  USER_ID: 'nxt_uid',
} as const;

let inMemoryAccessToken: string | null = null;
let inMemoryRefreshToken: string | null = null;
let inMemoryCsrfToken: string | null = null;

/**
 * Refresh token mantido em memória — nunca persistido no browser storage.
 * Em caso de refresh da página, o usuário precisa re-autenticar,
 * o que é o comportamento seguro esperado para tokens de longa duração.
 */
export function storeTokens(accessToken: string, refreshToken: string): void {
  inMemoryAccessToken = accessToken;
  inMemoryRefreshToken = refreshToken;
}

/**
 * Atualiza apenas o access token (usado após refresh silencioso).
 */
export function updateAccessToken(accessToken: string): void {
  inMemoryAccessToken = accessToken;
}

/**
 * Retorna o access token armazenado, ou null se não existir.
 */
export function getAccessToken(): string | null {
  return inMemoryAccessToken;
}

/**
 * Retorna o refresh token em memória, ou null se não existir.
 * Este token NUNCA é lido de storage do browser.
 */
export function getRefreshToken(): string | null {
  return inMemoryRefreshToken;
}

/**
 * Armazena o token CSRF em memória.
 */
export function storeCsrfToken(csrfToken: string): void {
  inMemoryCsrfToken = csrfToken;
}

/**
 * Retorna o token CSRF armazenado, ou null se não existir.
 */
export function getCsrfToken(): string | null {
  return inMemoryCsrfToken;
}

/**
 * Remove o token CSRF da memória.
 */
export function clearCsrfToken(): void {
  inMemoryCsrfToken = null;
}

/**
 * Armazena o tenant ID no sessionStorage (necessário para re-hidratação após refresh).
 */
export function storeTenantId(tenantId: string): void {
  sessionStorage.setItem(SESSION_KEYS.TENANT_ID, tenantId);
}

/**
 * Retorna o tenant ID armazenado, ou null se não existir.
 */
export function getTenantId(): string | null {
  return sessionStorage.getItem(SESSION_KEYS.TENANT_ID);
}

/**
 * Armazena o user ID no sessionStorage (necessário para fallback de perfil).
 */
export function storeUserId(userId: string): void {
  sessionStorage.setItem(SESSION_KEYS.USER_ID, userId);
}

/**
 * Retorna o user ID armazenado, ou null se não existir.
 */
export function getUserId(): string | null {
  return sessionStorage.getItem(SESSION_KEYS.USER_ID);
}

/**
 * Remove todos os dados de sessão do storage e da memória.
 * Deve ser chamado no logout e em caso de sessão inválida.
 */
export function clearAllTokens(): void {
  sessionStorage.removeItem(SESSION_KEYS.TENANT_ID);
  sessionStorage.removeItem(SESSION_KEYS.USER_ID);
  inMemoryAccessToken = null;
  inMemoryRefreshToken = null;
  inMemoryCsrfToken = null;
}

/**
 * Verifica se existe um access token armazenado (indica sessão potencialmente ativa).
 * Não valida expiração — a validação real é feita pelo backend.
 */
export function hasActiveSession(): boolean {
  return !!inMemoryAccessToken || !!getTenantId();
}

/**
 * Migra tokens de localStorage para sessionStorage durante a transição.
 * Remove os dados antigos do localStorage após migração.
 * Chamado uma única vez na inicialização para compatibilidade retroativa.
 */
export function migrateFromLocalStorage(): void {
  const legacyKeys = ['access_token', 'refresh_token', 'tenant_id', 'user_id'];
  const oldTenantId = localStorage.getItem('tenant_id');
  const oldUserId = localStorage.getItem('user_id');

  if (oldTenantId) {
    sessionStorage.setItem(SESSION_KEYS.TENANT_ID, oldTenantId);
  }

  if (oldUserId) {
    sessionStorage.setItem(SESSION_KEYS.USER_ID, oldUserId);
  }

  for (const key of legacyKeys) {
    localStorage.removeItem(key);
  }
}
