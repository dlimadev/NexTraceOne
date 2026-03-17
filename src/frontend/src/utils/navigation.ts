/**
 * Utilitários de navegação segura para prevenção de open redirect.
 *
 * Open redirect ocorre quando a aplicação redireciona para uma URL
 * controlada por input externo sem validação. Isso pode ser explorado
 * em ataques de phishing combinados com a confiança do domínio legítimo.
 *
 * Todas as funções de redirecionamento da aplicação devem usar este módulo
 * para garantir que destinos são validados contra a lista de rotas internas.
 */

/**
 * Rotas internas válidas da aplicação.
 * Qualquer redirecionamento deve apontar para uma dessas rotas
 * ou para um caminho que comece com um desses prefixos.
 */
const ALLOWED_INTERNAL_PATHS = [
  '/',
  '/login',
  '/select-tenant',
  '/services',
  '/releases',
  '/graph',
  '/contracts',
  '/workflow',
  '/promotion',
  '/portal',
  '/operations',
  '/ai',
  '/governance',
  '/users',
  '/audit',
  '/break-glass',
  '/jit-access',
  '/delegations',
  '/access-reviews',
  '/my-sessions',
  '/unauthorized',
] as const;

/**
 * Valida se um caminho de redirecionamento é seguro (interno à aplicação).
 *
 * Regras de validação:
 * - Deve ser um caminho relativo (começar com '/')
 * - Não pode começar com '//' (protocol-relative URL, vetor de open redirect)
 * - Não pode conter '://' (URL absoluta disfarçada)
 * - Não pode conter caracteres de controle ou encoded sequences perigosas
 * - Deve corresponder a uma rota interna conhecida
 *
 * @param path - Caminho a ser validado
 * @returns true se o caminho é considerado seguro para redirecionamento
 */
export function isSafeRedirectPath(path: string): boolean {
  if (!path || typeof path !== 'string') return false;

  // Verificar caracteres de controle no input original (antes de trim)
  // eslint-disable-next-line no-control-regex
  if (/[\x00-\x1f\x7f]/.test(path)) return false;

  const trimmed = path.trim();

  // Deve começar com '/' (caminho relativo)
  if (!trimmed.startsWith('/')) return false;

  // Não pode ser protocol-relative URL (//evil.com)
  if (trimmed.startsWith('//')) return false;

  // Não pode conter scheme (javascript:, data:, http://, etc.)
  if (trimmed.includes('://')) return false;

  // Extrai apenas o pathname (remove query string e hash)
  const pathname = trimmed.split('?')[0].split('#')[0];

  // Verifica se o pathname corresponde a uma rota interna conhecida
  return ALLOWED_INTERNAL_PATHS.some(
    (allowed) => pathname === allowed || pathname.startsWith(allowed + '/')
  );
}

/**
 * Retorna um caminho de redirecionamento seguro.
 * Se o caminho fornecido não for seguro, retorna o fallback (padrão: '/').
 *
 * @param path - Caminho desejado
 * @param fallback - Caminho fallback seguro (padrão: '/')
 * @returns Caminho validado e seguro para redirecionamento
 */
export function getSafeRedirectPath(path: string, fallback = '/'): string {
  return isSafeRedirectPath(path) ? path.trim() : fallback;
}

/**
 * Verifica se uma URL é externa (aponta para outro domínio).
 * URLs externas devem ser tratadas com cuidado adicional
 * (ex.: abrir em nova aba, exibir aviso ao usuário).
 *
 * @param url - URL a verificar
 * @returns true se a URL aponta para domínio externo
 */
export function isExternalUrl(url: string): boolean {
  if (!url) return false;
  try {
    const parsed = new URL(url, window.location.origin);
    return parsed.origin !== window.location.origin;
  } catch {
    // Se não for parseable como URL, não é externa
    return false;
  }
}
