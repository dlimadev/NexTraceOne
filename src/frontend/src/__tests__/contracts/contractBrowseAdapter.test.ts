/**
 * Testes TDD para o adapter puro de browse de contratos.
 * Cobrem: facetas (contagem por campo), exclusão de vazio, filtros (AND/OR), ordenação.
 */
import { describe, it, expect } from 'vitest';
import {
  computeContractFacets,
  filterContracts,
  sortContracts,
} from '../../features/contracts/catalog/browse/contractBrowseAdapter';
import type { CatalogItem } from '../../features/contracts/catalog/types';
import { EMPTY_CONTRACT_BROWSE_FILTERS } from '../../features/contracts/catalog/browse/contractBrowseTypes';

// ── Fixtures ─────────────────────────────────────────────────────────────────

function makeItem(overrides: Partial<CatalogItem>): CatalogItem {
  return {
    // Required ContractListItem fields
    apiAssetId: 'api-001',
    protocol: 'OpenApi',
    lifecycleState: 'Approved',
    // Required CatalogItem fields
    name: 'Service A',
    semVer: '1.0.0',
    domain: 'Payments',
    team: 'Team Alpha',
    technicalOwner: 'owner@example.com',
    criticality: 'Medium',
    exposure: 'Internal',
    updatedAt: '2026-01-15T10:00:00Z',
    catalogServiceType: 'RestApi',
    approvalState: 'Approved',
    ...overrides,
  } as CatalogItem;
}

const ITEMS: CatalogItem[] = [
  makeItem({
    apiAssetId: 'api-001',
    name: 'Alpha API',
    domain: 'Payments',
    team: 'Team Alpha',
    criticality: 'High',
    exposure: 'Public',
    catalogServiceType: 'RestApi',
    lifecycleState: 'Approved',
    approvalState: 'Approved',
    updatedAt: '2026-03-01T00:00:00Z',
  }),
  makeItem({
    apiAssetId: 'api-002',
    name: 'Beta API',
    domain: 'Payments',
    team: 'Team Beta',
    criticality: 'Medium',
    exposure: 'Internal',
    catalogServiceType: 'RestApi',
    lifecycleState: 'Draft',
    approvalState: 'Pending',
    updatedAt: '2026-01-10T00:00:00Z',
  }),
  makeItem({
    apiAssetId: 'api-003',
    name: 'Gamma Event',
    domain: 'Orders',
    team: 'Team Alpha',
    criticality: 'Low',
    exposure: 'Internal',
    catalogServiceType: 'Event',
    lifecycleState: 'Deprecated',
    approvalState: 'Approved',
    updatedAt: '2025-12-01T00:00:00Z',
  }),
  makeItem({
    apiAssetId: 'api-004',
    name: 'Delta SOAP',
    domain: '',       // empty domain — must NOT enter facet
    team: '',         // empty team  — must NOT enter facet
    criticality: 'High',
    exposure: 'Public',
    catalogServiceType: 'Soap',
    lifecycleState: 'Approved',
    approvalState: 'InReview',
    updatedAt: '2026-02-20T00:00:00Z',
  }),
];

// ── computeContractFacets ─────────────────────────────────────────────────────

describe('computeContractFacets', () => {
  it('counts serviceType correctly', () => {
    const facets = computeContractFacets(ITEMS);
    const restApi = facets.serviceTypes.find(f => f.value === 'RestApi');
    expect(restApi?.count).toBe(2);
    const event = facets.serviceTypes.find(f => f.value === 'Event');
    expect(event?.count).toBe(1);
    const soap = facets.serviceTypes.find(f => f.value === 'Soap');
    expect(soap?.count).toBe(1);
  });

  it('counts lifecycleState correctly', () => {
    const facets = computeContractFacets(ITEMS);
    const approved = facets.lifecycles.find(f => f.value === 'Approved');
    expect(approved?.count).toBe(2);
    const draft = facets.lifecycles.find(f => f.value === 'Draft');
    expect(draft?.count).toBe(1);
  });

  it('counts domain correctly and excludes empty strings', () => {
    const facets = computeContractFacets(ITEMS);
    // 'Payments' × 2, 'Orders' × 1; empty string (api-004) excluded
    const payments = facets.domains.find(f => f.value === 'Payments');
    expect(payments?.count).toBe(2);
    const orders = facets.domains.find(f => f.value === 'Orders');
    expect(orders?.count).toBe(1);
    // No facet with empty-string value
    const emptyEntry = facets.domains.find(f => f.value === '');
    expect(emptyEntry).toBeUndefined();
    // Total entries: 2 (Payments, Orders)
    expect(facets.domains).toHaveLength(2);
  });

  it('excludes empty team from facet', () => {
    const facets = computeContractFacets(ITEMS);
    const emptyTeam = facets.teams.find(f => f.value === '');
    expect(emptyTeam).toBeUndefined();
    // Two distinct non-empty teams: Alpha, Beta
    expect(facets.teams).toHaveLength(2);
  });

  it('sorts each facet group by count descending', () => {
    const facets = computeContractFacets(ITEMS);
    // RestApi (2) should come before Event (1) and Soap (1)
    expect(facets.serviceTypes[0].value).toBe('RestApi');
    expect(facets.serviceTypes[0].count).toBe(2);
    // Payments (2) before Orders (1)
    expect(facets.domains[0].value).toBe('Payments');
  });

  it('returns facet with value === label', () => {
    const facets = computeContractFacets(ITEMS);
    for (const f of facets.serviceTypes) {
      expect(f.label).toBe(f.value);
    }
  });

  it('returns empty arrays when items list is empty', () => {
    const facets = computeContractFacets([]);
    expect(facets.serviceTypes).toHaveLength(0);
    expect(facets.domains).toHaveLength(0);
    expect(facets.lifecycles).toHaveLength(0);
  });
});

// ── filterContracts ───────────────────────────────────────────────────────────

describe('filterContracts', () => {
  it('returns all items with empty filters', () => {
    const result = filterContracts(ITEMS, EMPTY_CONTRACT_BROWSE_FILTERS);
    expect(result).toHaveLength(4);
  });

  it('filters by q substring on name (case-insensitive)', () => {
    // 'delta' only appears in "Delta SOAP" — no other field of any item contains it
    const result = filterContracts(ITEMS, { ...EMPTY_CONTRACT_BROWSE_FILTERS, q: 'delta' });
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Delta SOAP');
  });

  it('q matches across name AND team (substring over all haystack fields)', () => {
    // 'alpha' appears in name of api-001 ("Alpha API") and team of api-003 ("Team Alpha")
    const result = filterContracts(ITEMS, { ...EMPTY_CONTRACT_BROWSE_FILTERS, q: 'alpha' });
    expect(result).toHaveLength(2);
    expect(result.map(i => i.apiAssetId).sort()).toEqual(['api-001', 'api-003']);
  });

  it('filters by q substring on domain', () => {
    const result = filterContracts(ITEMS, { ...EMPTY_CONTRACT_BROWSE_FILTERS, q: 'orders' });
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Gamma Event');
  });

  it('filters by q substring on technicalOwner', () => {
    const items = [
      makeItem({ apiAssetId: 'x1', name: 'X1', technicalOwner: 'alice@example.com' }),
      makeItem({ apiAssetId: 'x2', name: 'X2', technicalOwner: 'bob@example.com' }),
    ];
    const result = filterContracts(items, { ...EMPTY_CONTRACT_BROWSE_FILTERS, q: 'alice' });
    expect(result).toHaveLength(1);
    expect(result[0].apiAssetId).toBe('x1');
  });

  it('applies serviceTypes filter (OR within group)', () => {
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      serviceTypes: ['Event', 'Soap'],
    });
    expect(result).toHaveLength(2);
    expect(result.map(i => i.catalogServiceType).sort()).toEqual(['Event', 'Soap']);
  });

  it('applies lifecycles filter', () => {
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      lifecycles: ['Draft'],
    });
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Beta API');
  });

  it('applies domains filter', () => {
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      domains: ['Orders'],
    });
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Gamma Event');
  });

  it('applies AND logic across different filter groups', () => {
    // serviceType RestApi AND domain Payments → api-001 (Alpha API) only
    // api-002 (Beta API) is also RestApi + Payments, but let's check
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      serviceTypes: ['RestApi'],
      domains: ['Orders'],
    });
    // Orders only has Event (Gamma), so RestApi AND Orders = 0
    expect(result).toHaveLength(0);
  });

  it('applies AND logic: q + facet filter', () => {
    // q='API' matches Alpha API, Beta API, Delta SOAP (has 'API' in name? no)
    // actually 'API' matches Alpha API and Beta API in name
    // then serviceTypes=['RestApi'] keeps both
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      q: 'api',
      serviceTypes: ['RestApi'],
    });
    // Alpha API (RestApi, name has 'API') + Beta API (RestApi, name has 'API') = 2
    expect(result).toHaveLength(2);
  });

  it('applies teams filter', () => {
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      teams: ['Team Alpha'],
    });
    expect(result).toHaveLength(2);
    expect(result.map(i => i.name).sort()).toEqual(['Alpha API', 'Gamma Event']);
  });

  it('applies criticalities filter', () => {
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      criticalities: ['High'],
    });
    expect(result).toHaveLength(2); // Alpha API + Delta SOAP
  });

  it('applies exposures filter', () => {
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      exposures: ['Public'],
    });
    expect(result).toHaveLength(2); // Alpha API + Delta SOAP
  });

  it('applies approvals filter', () => {
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      approvals: ['InReview'],
    });
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Delta SOAP');
  });

  it('OR within group: approvals InReview + Pending', () => {
    const result = filterContracts(ITEMS, {
      ...EMPTY_CONTRACT_BROWSE_FILTERS,
      approvals: ['InReview', 'Pending'],
    });
    expect(result).toHaveLength(2);
  });
});

// ── sortContracts ─────────────────────────────────────────────────────────────

describe('sortContracts', () => {
  it('sort by name produces A→Z order', () => {
    const result = sortContracts(ITEMS, 'name');
    const names = result.map(i => i.name);
    expect(names).toEqual(['Alpha API', 'Beta API', 'Delta SOAP', 'Gamma Event']);
  });

  it('sort by name does not mutate the input array', () => {
    const input = [...ITEMS];
    sortContracts(input, 'name');
    expect(input.map(i => i.name)).toEqual(ITEMS.map(i => i.name));
  });

  it('sort by updated produces newest-first (desc)', () => {
    const result = sortContracts(ITEMS, 'updated');
    const dates = result.map(i => i.updatedAt);
    // Expected: 2026-03-01, 2026-02-20, 2026-01-10, 2025-12-01
    expect(dates[0]).toBe('2026-03-01T00:00:00Z');
    expect(dates[dates.length - 1]).toBe('2025-12-01T00:00:00Z');
  });

  it('sort by criticality produces High→Medium→Low, unknown last', () => {
    const items = [
      makeItem({ apiAssetId: 'c1', name: 'C1', criticality: 'Low' }),
      makeItem({ apiAssetId: 'c2', name: 'C2', criticality: 'High' }),
      makeItem({ apiAssetId: 'c3', name: 'C3', criticality: 'Medium' }),
      makeItem({ apiAssetId: 'c4', name: 'C4', criticality: '' }),  // unknown → last
    ];
    const result = sortContracts(items, 'criticality');
    expect(result.map(i => i.criticality)).toEqual(['High', 'Medium', 'Low', '']);
  });

  it('sort by relevance preserves input order', () => {
    const result = sortContracts(ITEMS, 'relevance');
    expect(result.map(i => i.apiAssetId)).toEqual(ITEMS.map(i => i.apiAssetId));
  });
});
