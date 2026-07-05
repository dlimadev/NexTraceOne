import type { AssetGraph } from '../../../types';
import type { ServiceVM, ApiVM, Exposure, Lifecycle, CatalogFilters, FacetGroups, FacetCount, SortKey } from './catalogTypes';

/** Ordem de abertura: Internal < Partner < Public. */
const EXPOSURE_RANK: Record<Exposure, number> = { Internal: 0, Partner: 1, Public: 2 };

/**
 * Converte valor desconhecido para string não-vazia, ou undefined se inválido.
 */
const norm = (v: unknown): string | undefined => (typeof v === 'string' && v.trim() ? v : undefined);

const asExposure = (v: unknown): Exposure | undefined =>
  v === 'Public' || v === 'Internal' || v === 'Partner' ? v : undefined;

/**
 * Mapeamento de valores reais de LifecycleStatus (API) e valores sintéticos (testes)
 * para o union Lifecycle do view-model.
 *
 * Valores reais da API (type LifecycleStatus em types/index.ts):
 *   'Planning' | 'Development' | 'Staging' | 'Active' | 'Deprecating' | 'Deprecated' | 'Retired'
 *
 * Mapeamento adoptado:
 *   Active       → Stable   (serviço em produção e estável)
 *   Planning     → Beta     (pré-produção)
 *   Development  → Beta     (pré-produção)
 *   Staging      → Beta     (pré-produção)
 *   Deprecating  → Deprecated (em processo de descontinuação)
 *   Deprecated   → Deprecated (já descontinuado)
 *   Retired      → Unknown  (retirado; sem correspondência direta)
 *
 * Valores sintéticos dos testes passam diretamente:
 *   Stable | Beta | Deprecated → passthrough
 */
const LIFECYCLE_MAP: Record<string, Lifecycle> = {
  // Passthrough (valores usados nos testes)
  Stable: 'Stable',
  Beta: 'Beta',
  Deprecated: 'Deprecated',
  // Valores reais da LifecycleStatus da API
  Active: 'Stable',
  Planning: 'Beta',
  Development: 'Beta',
  Staging: 'Beta',
  Deprecating: 'Deprecated',
  Retired: 'Unknown',
};

const asLifecycle = (v: unknown): Lifecycle =>
  typeof v === 'string' && v in LIFECYCLE_MAP ? LIFECYCLE_MAP[v] : 'Unknown';

/**
 * Transforma o grafo de activos (AssetGraph) num array de ServiceVM.
 *
 * Acesso defensivo via Record<string, unknown> para cobrir tanto os tipos reais
 * (ServiceNode / ApiNode) como os objectos sintéticos usados nos testes,
 * que são passados com "as never" e podem conter campos extra não presentes
 * nos tipos de API (ex.: lifecycle, hasContract, serviceAssetId em ApiNode).
 */
export function toServiceVMs(graph: AssetGraph): ServiceVM[] {
  const apisByService = new Map<string, ApiVM[]>();

  for (const rawApi of graph.apis ?? []) {
    const a = rawApi as unknown as Record<string, unknown>;
    const vm: ApiVM = {
      id: norm(a['apiAssetId']) ?? norm(a['id']) ?? '',
      name: typeof a['name'] === 'string' ? a['name'] : '',
      routePattern: norm(a['routePattern']),
      protocol: norm(a['protocol']) ?? norm(a['interfaceType']),
      exposure: asExposure(a['visibility'] ?? a['exposure']),
      version: norm(a['version']),
      hasContract: Boolean(a['hasContract'] ?? (typeof a['contractCount'] === 'number' ? a['contractCount'] > 0 : false)),
      consumerCount: Array.isArray(a['consumers'])
        ? a['consumers'].length
        : typeof a['consumerCount'] === 'number'
          ? a['consumerCount']
          : undefined,
    };
    // Suporta 'serviceAssetId' (mocks) e 'ownerServiceAssetId' (API real).
    const key = norm(a['serviceAssetId']) ?? norm(a['ownerServiceAssetId']) ?? norm(a['serviceId']) ?? '';
    if (!apisByService.has(key)) apisByService.set(key, []);
    apisByService.get(key)!.push(vm);
  }

  return (graph.services ?? []).map((rawSvc) => {
    const s = rawSvc as unknown as Record<string, unknown>;
    const svcId = norm(s['serviceAssetId']) ?? norm(s['id']) ?? '';
    const apis = apisByService.get(svcId) ?? [];

    // Exposição agregada: a API mais aberta determina a exposição do serviço.
    const exposure =
      apis
        .map((a) => a.exposure)
        .filter((e): e is Exposure => !!e)
        .sort((x, y) => EXPOSURE_RANK[y] - EXPOSURE_RANK[x])[0] ??
      asExposure(s['exposure'] ?? s['visibility']);

    // Ciclo de vida: lê 'lifecycle' (testes), 'lifecycleStatus' (API real), ou 'stage'.
    const lifecycle = asLifecycle(s['lifecycle'] ?? s['lifecycleStatus'] ?? s['stage']);

    // Saúde: apenas valores conhecidos; honest-null caso ausente.
    const healthVal = s['health'];
    const health =
      healthVal === 'Ok' || healthVal === 'Warn' || healthVal === 'Down' ? healthVal : undefined;

    return {
      id: svcId,
      name: typeof s['name'] === 'string' ? s['name'] : '',
      description: norm(s['capability']) ?? norm(s['description']),
      domain: norm(s['domain']),
      team: norm(s['teamName']) ?? norm(s['team']),
      owner: norm(s['technicalOwner']) ?? norm(s['owner']),
      lifecycle,
      exposure,
      health,
      apis,
      contractCount: apis.filter((a) => a.hasContract).length,
    };
  });
}

const countBy = (items: (string | undefined)[]): FacetCount[] => {
  const m = new Map<string, number>();
  for (const v of items) if (v) m.set(v, (m.get(v) ?? 0) + 1);
  return [...m.entries()]
    .map(([value, count]) => ({ value, label: value, count }))
    .sort((a, b) => b.count - a.count);
};

export function computeFacets(services: ServiceVM[]): FacetGroups {
  return {
    domains:   countBy(services.map((s) => s.domain)),
    protocols: countBy(services.flatMap((s) => s.apis.map((a) => a.protocol))),
    exposures: countBy(services.map((s) => s.exposure)),
    lifecycles: countBy(services.map((s) => s.lifecycle)),
    teams:     countBy(services.map((s) => s.team)),
  };
}

export function filterServices(services: ServiceVM[], f: CatalogFilters): ServiceVM[] {
  const q = f.q.trim().toLowerCase();
  return services.filter((s) => {
    if (q) {
      const hay = [s.name, s.description, s.domain, s.team, ...s.apis.map((a) => `${a.name} ${a.routePattern ?? ''}`)]
        .join(' ')
        .toLowerCase();
      if (!hay.includes(q)) return false;
    }
    if (f.domains.length && !(s.domain && f.domains.includes(s.domain))) return false;
    if (f.teams.length && !(s.team && f.teams.includes(s.team))) return false;
    if (f.exposures.length && !(s.exposure && f.exposures.includes(s.exposure))) return false;
    if (f.lifecycles.length && !f.lifecycles.includes(s.lifecycle)) return false;
    if (f.protocols.length && !s.apis.some((a) => a.protocol && f.protocols.includes(a.protocol))) return false;
    if (f.hasContract === true && s.contractCount === 0) return false;
    if (f.hasContract === false && s.contractCount > 0) return false;
    return true;
  });
}

export function sortServices(services: ServiceVM[], key: SortKey): ServiceVM[] {
  const arr = [...services];
  if (key === 'name') {
    arr.sort((a, b) => a.name.localeCompare(b.name));
  } else if (key === 'consumers') {
    arr.sort(
      (a, b) =>
        b.apis.reduce((n, x) => n + (x.consumerCount ?? 0), 0) -
        a.apis.reduce((n, x) => n + (x.consumerCount ?? 0), 0),
    );
  }
  // 'relevance' e 'recent' preservam a ordem da API por agora.
  return arr;
}

/** Achata todas as ApiVM de uma lista de ServiceVM (usado no modo "APIs"). */
export function toApiVMs(services: ServiceVM[]): ApiVM[] {
  return services.flatMap((s) => s.apis);
}
