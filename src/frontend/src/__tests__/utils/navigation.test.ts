import { describe, it, expect } from 'vitest';
import { isSafeRedirectPath, getSafeRedirectPath, isExternalUrl } from '../../utils/navigation';

/**
 * Testes de segurança para utilitários de navegação — prevenção de open redirect.
 *
 * Open redirect é uma vulnerabilidade crítica em aplicações web que permite
 * atacantes redirecionarem usuários para sites maliciosos usando o domínio legítimo.
 * Estes testes validam que todos os vetores conhecidos de open redirect são bloqueados.
 */
describe('isSafeRedirectPath', () => {
  it('aceita rota raiz', () => {
    expect(isSafeRedirectPath('/')).toBe(true);
  });

  it('aceita rotas internas conhecidas', () => {
    expect(isSafeRedirectPath('/releases')).toBe(true);
    expect(isSafeRedirectPath('/graph')).toBe(true);
    expect(isSafeRedirectPath('/contracts')).toBe(true);
    expect(isSafeRedirectPath('/workflow')).toBe(true);
    expect(isSafeRedirectPath('/promotion')).toBe(true);
    expect(isSafeRedirectPath('/users')).toBe(true);
    expect(isSafeRedirectPath('/audit')).toBe(true);
    expect(isSafeRedirectPath('/login')).toBe(true);
    expect(isSafeRedirectPath('/unauthorized')).toBe(true);
  });

  it('rejeita protocol-relative URLs (//evil.com)', () => {
    expect(isSafeRedirectPath('//evil.com')).toBe(false);
    expect(isSafeRedirectPath('//evil.com/login')).toBe(false);
  });

  it('rejeita URLs absolutas com scheme', () => {
    expect(isSafeRedirectPath('http://evil.com')).toBe(false);
    expect(isSafeRedirectPath('https://evil.com')).toBe(false);
    expect(isSafeRedirectPath('ftp://evil.com')).toBe(false);
  });

  it('rejeita javascript: URIs', () => {
    expect(isSafeRedirectPath('javascript:alert(1)')).toBe(false);
  });

  it('rejeita data: URIs', () => {
    expect(isSafeRedirectPath('data:text/html,<script>alert(1)</script>')).toBe(false);
  });

  it('rejeita strings vazias e null-like', () => {
    expect(isSafeRedirectPath('')).toBe(false);
    // @ts-expect-error — testando input inválido
    expect(isSafeRedirectPath(null)).toBe(false);
    // @ts-expect-error — testando input inválido
    expect(isSafeRedirectPath(undefined)).toBe(false);
  });

  it('rejeita caracteres de controle', () => {
    expect(isSafeRedirectPath('/login\x00')).toBe(false);
    expect(isSafeRedirectPath('/login\x0d\x0a')).toBe(false);
  });

  it('rejeita rotas desconhecidas', () => {
    expect(isSafeRedirectPath('/admin')).toBe(false);
    expect(isSafeRedirectPath('/secret')).toBe(false);
    expect(isSafeRedirectPath('/api/v1/users')).toBe(false);
  });

  it('aceita sub-rotas de rotas internas', () => {
    expect(isSafeRedirectPath('/releases/123')).toBe(true);
    expect(isSafeRedirectPath('/users/create')).toBe(true);
  });

  it('rejeita caminhos que não começam com /', () => {
    expect(isSafeRedirectPath('releases')).toBe(false);
    expect(isSafeRedirectPath('login')).toBe(false);
  });
});

describe('getSafeRedirectPath', () => {
  it('retorna o caminho quando seguro', () => {
    expect(getSafeRedirectPath('/releases')).toBe('/releases');
  });

  it('retorna fallback para caminhos inseguros', () => {
    expect(getSafeRedirectPath('//evil.com')).toBe('/');
    expect(getSafeRedirectPath('https://evil.com')).toBe('/');
  });

  it('retorna fallback customizado', () => {
    expect(getSafeRedirectPath('//evil.com', '/login')).toBe('/login');
  });
});

describe('isExternalUrl', () => {
  it('identifica URLs externas', () => {
    expect(isExternalUrl('https://evil.com')).toBe(true);
    expect(isExternalUrl('http://other-domain.com/path')).toBe(true);
  });

  it('identifica URLs internas como não externas', () => {
    expect(isExternalUrl(window.location.origin + '/path')).toBe(false);
    expect(isExternalUrl('/relative-path')).toBe(false);
  });

  it('trata strings vazias como não externas', () => {
    expect(isExternalUrl('')).toBe(false);
  });
});
