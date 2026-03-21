export const finalProductionIncludedRoutePrefixes = [
  '/',
  '/login',
  '/forgot-password',
  '/reset-password',
  '/activate',
  '/mfa',
  '/invitation',
  '/select-tenant',
  '/search',
  '/source-of-truth',
  '/services',
  '/graph',
  '/contracts',
  '/changes',
  '/releases',
  '/workflow',
  '/promotion',
  '/operations',
  '/ai',
  '/users',
  '/audit',
  '/break-glass',
  '/jit-access',
  '/delegations',
  '/access-reviews',
  '/my-sessions',
  '/unauthorized',
  '/portal',
  '/governance',
  '/integrations',
  '/analytics',
  '/platform',
] as const;

export const finalProductionExcludedRoutePrefixes = [] as const;

function normalizeRoute(route: string): string {
  if (!route) {
    return '/';
  }

  const normalized = route.startsWith('/') ? route : `/${route}`;
  if (normalized.length > 1 && normalized.endsWith('/')) {
    return normalized.slice(0, -1);
  }

  return normalized;
}

function matchesPrefix(route: string, prefix: string): boolean {
  if (prefix === '/') {
    return route === '/';
  }

  return route === prefix || route.startsWith(`${prefix}/`);
}

export function isRouteIncludedInFinalProductionScope(route: string): boolean {
  const normalized = normalizeRoute(route);
  return finalProductionIncludedRoutePrefixes.some((prefix) => matchesPrefix(normalized, prefix));
}

export function isRouteExplicitlyExcludedFromFinalProductionScope(route: string): boolean {
  const normalized = normalizeRoute(route);
  return finalProductionExcludedRoutePrefixes.some((prefix) => matchesPrefix(normalized, prefix));
}

export function isRouteAvailableInFinalProductionScope(route: string): boolean {
  return isRouteIncludedInFinalProductionScope(route) && !isRouteExplicitlyExcludedFromFinalProductionScope(route);
}
