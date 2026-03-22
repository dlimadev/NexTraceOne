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

  // Phase 4: AI Governance routes promoted to production scope
  it.each([
    '/ai/models',
    '/ai/models/123',
    '/ai/policies',
    '/ai/policies/edit',
    '/ai/routing',
    '/ai/routing/strategies',
    '/ai/ide',
    '/ai/ide/clients',
    '/ai/budgets',
    '/ai/budgets/detail',
    '/ai/audit',
    '/ai/audit/entries',
  ])('keeps AI Governance route %s available in final production scope (Phase 4)', (route) => {
    expect(isRouteExplicitlyExcludedFromFinalProductionScope(route)).toBe(false);
    expect(isRouteAvailableInFinalProductionScope(route)).toBe(true);
  });
});
