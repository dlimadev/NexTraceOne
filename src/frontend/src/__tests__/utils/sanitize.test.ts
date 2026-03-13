import { describe, it, expect } from 'vitest';
import { isSafeUrl, sanitizeUrl, escapeHtml } from '../../utils/sanitize';

/**
 * Testes de segurança para utilitários de sanitização — prevenção de XSS via URLs e HTML.
 *
 * Estes testes validam que URLs com esquemas perigosos (javascript:, data:, vbscript:)
 * são bloqueadas, e que funções de escape HTML funcionam corretamente.
 */
describe('isSafeUrl', () => {
  it('aceita URLs https', () => {
    expect(isSafeUrl('https://example.com')).toBe(true);
  });

  it('aceita URLs http', () => {
    expect(isSafeUrl('http://example.com')).toBe(true);
  });

  it('aceita URLs mailto', () => {
    expect(isSafeUrl('mailto:user@example.com')).toBe(true);
  });

  it('aceita caminhos relativos', () => {
    expect(isSafeUrl('/path/to/page')).toBe(true);
    expect(isSafeUrl('/releases/123')).toBe(true);
  });

  it('aceita âncoras (#)', () => {
    expect(isSafeUrl('#section')).toBe(true);
  });

  it('rejeita javascript: URIs', () => {
    expect(isSafeUrl('javascript:alert(1)')).toBe(false);
    expect(isSafeUrl('JavaScript:alert(1)')).toBe(false);
    expect(isSafeUrl('JAVASCRIPT:alert(1)')).toBe(false);
  });

  it('rejeita data: URIs', () => {
    expect(isSafeUrl('data:text/html,<script>alert(1)</script>')).toBe(false);
    expect(isSafeUrl('DATA:text/html;base64,xxx')).toBe(false);
  });

  it('rejeita vbscript: URIs', () => {
    expect(isSafeUrl('vbscript:MsgBox("XSS")')).toBe(false);
  });

  it('rejeita strings vazias', () => {
    expect(isSafeUrl('')).toBe(false);
    // @ts-expect-error — testando input inválido
    expect(isSafeUrl(null)).toBe(false);
    // @ts-expect-error — testando input inválido
    expect(isSafeUrl(undefined)).toBe(false);
  });

  it('rejeita URLs com caracteres de controle', () => {
    expect(isSafeUrl('https://example.com\x00')).toBe(false);
    expect(isSafeUrl('/path\x0d\x0a')).toBe(false);
  });

  it('rejeita protocol-relative URLs', () => {
    expect(isSafeUrl('//evil.com')).toBe(false);
  });

  it('rejeita esquemas desconhecidos', () => {
    expect(isSafeUrl('ftp://files.example.com')).toBe(false);
    expect(isSafeUrl('file:///etc/passwd')).toBe(false);
  });
});

describe('sanitizeUrl', () => {
  it('retorna URL segura inalterada', () => {
    expect(sanitizeUrl('https://example.com')).toBe('https://example.com');
  });

  it('retorna fallback para URLs inseguras', () => {
    expect(sanitizeUrl('javascript:alert(1)')).toBe('#');
  });

  it('aceita fallback customizado', () => {
    expect(sanitizeUrl('javascript:alert(1)', '/safe')).toBe('/safe');
  });
});

describe('escapeHtml', () => {
  it('escapa caracteres HTML especiais', () => {
    expect(escapeHtml('<script>alert("xss")</script>')).toBe(
      '&lt;script&gt;alert(&quot;xss&quot;)&lt;/script&gt;'
    );
  });

  it('escapa aspas simples', () => {
    expect(escapeHtml("it's a test")).toBe("it&#x27;s a test");
  });

  it('escapa & corretamente', () => {
    expect(escapeHtml('a & b')).toBe('a &amp; b');
  });

  it('retorna string vazia para input inválido', () => {
    expect(escapeHtml('')).toBe('');
    // @ts-expect-error — testando input inválido
    expect(escapeHtml(null)).toBe('');
    // @ts-expect-error — testando input inválido
    expect(escapeHtml(undefined)).toBe('');
  });

  it('não altera texto sem caracteres especiais', () => {
    expect(escapeHtml('hello world')).toBe('hello world');
  });
});
