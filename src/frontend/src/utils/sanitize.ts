/**
 * Utilitários de sanitização para prevenção de XSS e injeção de conteúdo.
 *
 * Este módulo centraliza funções de sanitização usadas em todo o frontend
 * para garantir que conteúdo dinâmico (vindo de APIs, query params, inputs)
 * seja tratado de forma segura antes de ser renderizado ou interpolado.
 *
 * Princípios:
 * - Nunca usar dangerouslySetInnerHTML com conteúdo não sanitizado.
 * - Nunca interpolar HTML/markdown externo diretamente no DOM.
 * - Sempre sanitizar URLs antes de usar em href/src/action.
 * - Validar esquemas de URL para prevenir javascript: e data: injection.
 */

/**
 * Esquemas de URL considerados seguros para uso em atributos href e src.
 * Qualquer esquema fora desta lista é bloqueado para prevenir
 * ataques de javascript:, data:, vbscript:, etc.
 */
const SAFE_URL_SCHEMES = ['https:', 'http:', 'mailto:'] as const;

/**
 * Valida se uma URL é segura para uso em atributos href/src.
 *
 * Bloqueia:
 * - javascript: URIs (vetor clássico de XSS)
 * - data: URIs (podem conter HTML/JS executável)
 * - vbscript: e outros esquemas perigosos
 * - URLs malformadas ou com caracteres de controle
 *
 * @param url - URL a validar
 * @returns true se a URL usa um esquema seguro
 */
export function isSafeUrl(url: string): boolean {
  if (!url || typeof url !== 'string') return false;

  const trimmed = url.trim().toLowerCase();

  // Bloqueia strings que começam com esquemas perigosos (case-insensitive)
  if (trimmed.startsWith('javascript:')) return false;
  if (trimmed.startsWith('data:')) return false;
  if (trimmed.startsWith('vbscript:')) return false;

  // Bloqueia caracteres de controle que podem bypassar validações
  // eslint-disable-next-line no-control-regex
  if (/[\x00-\x1f\x7f]/.test(url)) return false;

  // URLs relativas são seguras (mas não protocol-relative)
  if (trimmed.startsWith('/') && !trimmed.startsWith('//')) return true;
  if (trimmed.startsWith('#')) return true;

  // Protocol-relative URLs (//evil.com) são perigosas — bloquear explicitamente
  if (trimmed.startsWith('//')) return false;

  // Valida esquema para URLs absolutas
  try {
    const parsed = new URL(url, window.location.origin);
    // Se o host é diferente da origem atual, verificar se o scheme é seguro
    return (SAFE_URL_SCHEMES as readonly string[]).includes(parsed.protocol);
  } catch {
    // Se não puder ser parsed como URL, trata como possível caminho relativo
    return !trimmed.includes(':');
  }
}

/**
 * Sanitiza uma URL para uso seguro em atributos href/src.
 * Se a URL não for segura, retorna o fallback (padrão: '#').
 *
 * @param url - URL a sanitizar
 * @param fallback - Valor de retorno para URLs inseguras
 * @returns URL segura ou o fallback
 */
export function sanitizeUrl(url: string, fallback = '#'): string {
  return isSafeUrl(url) ? url : fallback;
}

/**
 * Escapa caracteres HTML especiais em uma string.
 * Útil quando é necessário inserir texto dinâmico em contextos HTML
 * fora do JSX (ex.: tooltips, títulos, atributos data-*).
 *
 * O React já escapa conteúdo JSX automaticamente,
 * mas esta função é necessária para contextos não-React.
 *
 * @param text - Texto a escapar
 * @returns Texto com caracteres HTML especiais escapados
 */
export function escapeHtml(text: string): string {
  if (!text || typeof text !== 'string') return '';

  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#x27;');
}
