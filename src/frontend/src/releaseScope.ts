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

/**
 * Routes excluded from the final production scope (ZR-6).
 * These are partial/planned/demo features not ready for production.
 * The route is available only if it matches an included prefix
 * AND does NOT match an excluded prefix.
 */
export const finalProductionExcludedRoutePrefixes = [
  '/portal',
  '/governance/teams',
  '/governance/packs',
  '/integrations/executions',
  '/analytics/value',
  '/operations/runbooks',
  '/operations/reliability',
  '/operations/automation',
  '/ai/models',
  '/ai/policies',
  '/ai/routing',
  '/ai/ide',
  '/ai/budgets',
  '/ai/audit',
] as const;

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
