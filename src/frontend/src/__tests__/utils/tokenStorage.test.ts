import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import {
  storeTokens,
  getAccessToken,
  getRefreshToken,
  clearAllTokens,
  hasActiveSession,
  getTenantId,
  storeTenantId,
  getUserId,
  storeUserId,
  migrateFromLocalStorage,
} from '../../utils/tokenStorage';

/**
 * Testes de segurança para o módulo de armazenamento de tokens.
 *
 * Valida que:
 * - O refresh token NUNCA é persistido em storage do browser
 * - O access token usa sessionStorage (escopo de aba)
 * - A limpeza de sessão remove todos os dados
 * - A migração de localStorage funciona e limpa dados antigos
 */
describe('tokenStorage — segurança de sessão', () => {
  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    clearAllTokens();
  });

  afterEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    clearAllTokens();
  });

  it('armazena access token em sessionStorage', () => {
    storeTokens('my-access-token', 'my-refresh-token');
    expect(sessionStorage.getItem('nxt_at')).toBe('my-access-token');
  });

  it('NÃO armazena refresh token em sessionStorage ou localStorage', () => {
    storeTokens('my-access-token', 'my-refresh-token');

    // Verifica que nenhuma chave de sessionStorage contém o refresh token
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i)!;
      expect(sessionStorage.getItem(key)).not.toBe('my-refresh-token');
    }

    // Verifica que nenhuma chave de localStorage contém o refresh token
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i)!;
      expect(localStorage.getItem(key)).not.toBe('my-refresh-token');
    }
  });

  it('mantém refresh token acessível via getRefreshToken (memória)', () => {
    storeTokens('access', 'refresh-secret');
    expect(getRefreshToken()).toBe('refresh-secret');
  });

  it('retorna access token corretamente', () => {
    storeTokens('access-123', 'refresh-456');
    expect(getAccessToken()).toBe('access-123');
  });

  it('clearAllTokens remove todos os dados de sessão', () => {
    storeTokens('access', 'refresh');
    storeTenantId('tenant-1');
    storeUserId('user-1');

    clearAllTokens();

    expect(getAccessToken()).toBeNull();
    expect(getRefreshToken()).toBeNull();
    expect(getTenantId()).toBeNull();
    expect(getUserId()).toBeNull();
    expect(sessionStorage.length).toBe(0);
  });

  it('hasActiveSession detecta sessão ativa', () => {
    expect(hasActiveSession()).toBe(false);
    storeTokens('access', 'refresh');
    expect(hasActiveSession()).toBe(true);
  });

  it('armazena e recupera tenant ID', () => {
    storeTenantId('tenant-abc');
    expect(getTenantId()).toBe('tenant-abc');
  });

  it('armazena e recupera user ID', () => {
    storeUserId('user-xyz');
    expect(getUserId()).toBe('user-xyz');
  });

  describe('migrateFromLocalStorage', () => {
    it('migra tokens de localStorage para sessionStorage/memória', () => {
      localStorage.setItem('access_token', 'legacy-access');
      localStorage.setItem('refresh_token', 'legacy-refresh');
      localStorage.setItem('tenant_id', 'legacy-tenant');
      localStorage.setItem('user_id', 'legacy-user');

      migrateFromLocalStorage();

      expect(getAccessToken()).toBe('legacy-access');
      expect(getRefreshToken()).toBe('legacy-refresh');
      expect(getTenantId()).toBe('legacy-tenant');
      expect(getUserId()).toBe('legacy-user');
    });

    it('remove dados antigos do localStorage após migração', () => {
      localStorage.setItem('access_token', 'old');
      localStorage.setItem('refresh_token', 'old');
      localStorage.setItem('tenant_id', 'old');
      localStorage.setItem('user_id', 'old');

      migrateFromLocalStorage();

      expect(localStorage.getItem('access_token')).toBeNull();
      expect(localStorage.getItem('refresh_token')).toBeNull();
      expect(localStorage.getItem('tenant_id')).toBeNull();
      expect(localStorage.getItem('user_id')).toBeNull();
    });

    it('não falha quando localStorage está vazio', () => {
      expect(() => migrateFromLocalStorage()).not.toThrow();
    });
  });
});
