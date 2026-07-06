/**
 * Adapter puro para browse de contratos — facetas, filtragem e ordenação.
 *
 * Funções sem efeitos secundários, testáveis em isolamento completo.
 * Espelha a estrutura de catalogAdapter.ts (services browse).
 */
import type { CatalogItem } from '../types';
import type {
  ContractBrowseFilters,
  ContractFacetCount,
  ContractFacetGroups,
  ContractSortKey,
} from './contractBrowseTypes';

// ── Rank map para ordenação por criticality ───────────────────────────────────

const CRITICALITY_RANK: Record<string, number> = {
  Critical: 0,
  High: 1,
  Medium: 2,
  Low: 3,
};

// ── Helpers internos ──────────────────────────────────────────────────────────

/**
 * Conta ocorrências de cada valor não-vazio e devolve ordenado por count desc.
 * Strings vazias ('') são ignoradas — "honest facets".
 */
const countBy = (values: string[]): ContractFacetCount[] => {
  const m = new Map<string, number>();
  for (const v of values) {
    if (v) m.set(v, (m.get(v) ?? 0) + 1);
  }
  return [...m.entries()]
    .map(([value, count]) => ({ value, label: value, count }))
    .sort((a, b) => b.count - a.count);
};

// ── API pública ───────────────────────────────────────────────────────────────

/**
 * Calcula os grupos de facetas a partir de uma lista de CatalogItem.
 *
 * Mapeamento de campo → grupo:
 *   catalogServiceType → serviceTypes
 *   lifecycleState     → lifecycles
 *   domain             → domains
 *   team               → teams
 *   criticality        → criticalities
 *   exposure           → exposures
 *   approvalState      → approvals
 *
 * Valores vazios ('') não entram em nenhuma faceta.
 * Cada grupo é ordenado por count desc.
 */
export function computeContractFacets(items: CatalogItem[]): ContractFacetGroups {
  return {
    serviceTypes: countBy(items.map(i => i.catalogServiceType)),
    lifecycles:   countBy(items.map(i => i.lifecycleState)),
    domains:      countBy(items.map(i => i.domain)),
    teams:        countBy(items.map(i => i.team)),
    criticalities: countBy(items.map(i => i.criticality)),
    exposures:    countBy(items.map(i => i.exposure)),
    approvals:    countBy(items.map(i => i.approvalState)),
  };
}

/**
 * Filtra contratos com lógica AND entre grupos e OR dentro de cada grupo.
 *
 * - `q` — substring case-insensitive sobre name, domain, team, technicalOwner, semVer
 * - Cada array de filtro: se não-vazio, o item deve corresponder a pelo menos um valor
 */
export function filterContracts(items: CatalogItem[], f: ContractBrowseFilters): CatalogItem[] {
  const q = f.q.trim().toLowerCase();

  return items.filter(item => {
    // Filtro textual (substring)
    if (q) {
      const hay = [item.name, item.domain, item.team, item.technicalOwner, item.semVer]
        .join(' ')
        .toLowerCase();
      if (!hay.includes(q)) return false;
    }

    // Filtros de faceta (AND entre grupos, OR dentro)
    if (f.serviceTypes.length && !f.serviceTypes.includes(item.catalogServiceType)) return false;
    if (f.lifecycles.length   && !f.lifecycles.includes(item.lifecycleState))       return false;
    if (f.domains.length      && !f.domains.includes(item.domain))                  return false;
    if (f.teams.length        && !f.teams.includes(item.team))                      return false;
    if (f.criticalities.length && !f.criticalities.includes(item.criticality))      return false;
    if (f.exposures.length    && !f.exposures.includes(item.exposure))              return false;
    if (f.approvals.length    && !f.approvals.includes(item.approvalState))         return false;

    return true;
  });
}

/**
 * Ordena contratos de forma pura (não muta o array de entrada).
 *
 * Chaves:
 *   name        → localeCompare A→Z
 *   updated     → updatedAt descending (mais recente primeiro)
 *   criticality → High > Medium > Low; valores desconhecidos vão para o fim
 *   relevance   → preserva a ordem de entrada
 */
export function sortContracts(items: CatalogItem[], key: ContractSortKey): CatalogItem[] {
  const arr = [...items];

  switch (key) {
    case 'name':
      arr.sort((a, b) => a.name.localeCompare(b.name));
      break;

    case 'updated':
      arr.sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
      break;

    case 'criticality':
      arr.sort((a, b) => {
        const ra = CRITICALITY_RANK[a.criticality] ?? 999;
        const rb = CRITICALITY_RANK[b.criticality] ?? 999;
        return ra - rb;
      });
      break;

    case 'relevance':
      // Preserva a ordem de entrada — nenhum sort necessário
      break;
  }

  return arr;
}
