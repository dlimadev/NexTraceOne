import { describe, expect, it } from 'vitest';
import {
  isRouteAvailableInFinalProductionScope,
  isRouteExplicitlyExcludedFromFinalProductionScope,
} from '../releaseScope';

describe('releaseScope ZR-6 final scope', () => {
  it.each([
    '/portal',
    '/portal/catalog',
    '/governance/teams',
    '/governance/packs/123',
    '/integrations/executions',
    '/analytics/value',
    '/operations/runbooks',
    '/operations/reliability',
    '/operations/automation/admin',
    '/ai/models',
    '/ai/policies',
    '/ai/routing',
    '/ai/ide',
    '/ai/budgets',
    '/ai/audit',
  ])('marks %s as explicitly removed from the final production scope', (route) => {
    expect(isRouteExplicitlyExcludedFromFinalProductionScope(route)).toBe(true);
    expect(isRouteAvailableInFinalProductionScope(route)).toBe(false);
  });

  it.each([
    '/services',
    '/source-of-truth',
    '/contracts',
    '/changes',
    '/operations/incidents',
    '/operations/incidents/abc',
    '/ai/assistant',
    '/users',
    '/audit',
  ])('keeps %s available in the final production scope', (route) => {
    expect(isRouteAvailableInFinalProductionScope(route)).toBe(true);
  });
});
