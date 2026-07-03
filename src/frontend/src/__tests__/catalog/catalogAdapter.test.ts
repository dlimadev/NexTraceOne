import { it, expect } from 'vitest';
import { toServiceVMs, computeFacets, filterServices, sortServices } from '../../features/catalog/browse/catalogAdapter';

const graph = {
  services: [
    { serviceAssetId: 's1', name: 'payment-service', domain: 'payments', teamName: 'Billing', capability: 'Pagamentos', lifecycle: 'Stable' },
    { serviceAssetId: 's2', name: 'legacy-x', domain: 'core', teamName: 'Core', lifecycle: 'Deprecated' },
  ],
  apis: [
    { apiAssetId: 'a1', name: 'REST payments', routePattern: '/payments', visibility: 'Internal', version: '1', consumers: ['c1','c2'], serviceAssetId: 's1', hasContract: true },
    { apiAssetId: 'a2', name: 'gRPC Pay', visibility: 'Public', serviceAssetId: 's1', hasContract: false },
  ],
} as never;

it('mapeia serviços com as suas apis e conta contratos', () => {
  const vms = toServiceVMs(graph);
  const s1 = vms.find(v => v.id === 's1')!;
  expect(s1.name).toBe('payment-service');
  expect(s1.apis).toHaveLength(2);
  expect(s1.contractCount).toBe(1);
  expect(s1.lifecycle).toBe('Stable');
});

it('agrega exposição do serviço a partir das apis (mais aberta vence)', () => {
  const s1 = toServiceVMs(graph).find(v => v.id === 's1')!;
  expect(s1.exposure).toBe('Public');
});

it('esconde sinais sem dado (honest-null)', () => {
  const s2 = toServiceVMs(graph).find(v => v.id === 's2')!;
  expect(s2.description).toBeUndefined();
  expect(s2.health).toBeUndefined();
});

it('computeFacets conta por domínio e ciclo', () => {
  const f = computeFacets(toServiceVMs(graph));
  expect(f.domains.find(d => d.value === 'payments')!.count).toBe(1);
  expect(f.lifecycles.find(l => l.value === 'Deprecated')!.count).toBe(1);
});

it('filterServices combina pesquisa + facetas (AND entre grupos, OR dentro)', () => {
  const vms = toServiceVMs(graph);
  const out = filterServices(vms, { q: 'pay', domains: ['payments'], protocols: [], exposures: [], lifecycles: [], hasContract: true, teams: [] });
  expect(out.map(s => s.id)).toEqual(['s1']);
});

it('sortServices por nome é estável e A→Z', () => {
  const vms = toServiceVMs(graph);
  expect(sortServices(vms, 'name').map(s => s.name)).toEqual(['legacy-x', 'payment-service']);
});
