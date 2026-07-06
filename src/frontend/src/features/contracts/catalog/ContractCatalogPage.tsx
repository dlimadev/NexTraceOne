import { useState, useMemo } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Layers, FileCheck, Clock, ShieldCheck, Lock, AlertTriangle, ListTree, Plus } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { FilterChip } from '../../../components/FilterChip';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer } from '../../../components/shell';
import { usePermissions } from '../../../hooks/usePermissions';
import { useContractList, useContractsSummary } from '../hooks';
import { CatalogTable } from './components';
import { ContractBrowseSurface } from './browse/ContractBrowseSurface';
import type { SortConfig, SortField, CatalogItem } from './types';
import { toCatalogItem } from './types';

/**
 * Catálogo governado de contratos — superfície de descoberta "browse-first"
 * com pesquisa, facetas e alternância Tabela|Cartões (ContractBrowseSurface).
 *
 * Suporta REST API, SOAP, Event API, Kafka Producer/Consumer e BackgroundService.
 * Dados enriquecidos (domain, team, owner, criticality) vêm do backend real.
 */
export function ContractCatalogPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { can } = usePermissions();

  // ── State ───────────────────────────────────────────────────────────────────
  // O sort próprio da tabela mantém-se; filtros/pesquisa vivem no URL (browse surface).
  const [sort, setSort] = useState<SortConfig>({ field: 'updatedAt', direction: 'desc' });

  // Ciclo de vida: fonte única de verdade = param `lifecycle` do URL (partilhado com a facet bar).
  const [searchParams, setSearchParams] = useSearchParams();
  const lifecycleParam = searchParams.get('lifecycle') ?? '';

  const setLifecycle = (state: string) =>
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (state) {
        next.set('lifecycle', state);
      } else {
        next.delete('lifecycle');
      }
      return next;
    });

  const toggleLifecycle = (state: string) =>
    setLifecycle(lifecycleParam === state ? '' : state);

  // ── Data fetching ───────────────────────────────────────────────────────────
  // Fetch sem filtros — o ContractBrowseSurface filtra client-side via estado do URL.
  const listQuery = useContractList();

  const summaryQuery = useContractsSummary();
  const summary = summaryQuery.data;

  // ── Transform to catalog items ──────────────────────────────────────────────
  const catalogItems = useMemo(
    () => (listQuery.data?.items ?? []).map(toCatalogItem),
    [listQuery.data],
  );

  // ── Render ──────────────────────────────────────────────────────────────────
  return (
    <PageContainer>
      {/* Cabeçalho com CTA primário no topo — padrão Betterstack */}
      <PageHeader
        title={t('contracts.catalog.title', 'Contract Catalog')}
        subtitle={t('contracts.catalog.subtitle', 'Governed catalog of all contracts, APIs, and service definitions.')}
        actions={
          can('contracts:write') ? (
            <Button
              variant="primary"
              size="sm"
              icon={<Plus size={14} />}
              onClick={() => navigate('/contracts/new')}
            >
              {t('contracts.catalog.actions.newContract', 'New contract')}
            </Button>
          ) : undefined
        }
      />

      {/* Summary chips — sincronizados com o param `lifecycle` do URL */}
      {summary && (
        <div className="flex items-center gap-2 flex-wrap">
          <FilterChip
            icon={<Layers size={12} />}
            label={t('contracts.catalog.summary.total', 'Total')}
            count={summary.totalVersions}
            active={lifecycleParam === ''}
            onClick={() => setLifecycle('')}
          />
          <FilterChip
            icon={<FileCheck size={12} />}
            label={t('contracts.catalog.summary.drafts', 'Drafts')}
            count={summary.draftCount}
            active={lifecycleParam === 'Draft'}
            onClick={() => toggleLifecycle('Draft')}
          />
          <FilterChip
            icon={<Clock size={12} />}
            label={t('contracts.catalog.summary.inReview', 'In Review')}
            count={summary.inReviewCount}
            active={lifecycleParam === 'InReview'}
            onClick={() => toggleLifecycle('InReview')}
          />
          <FilterChip
            icon={<ShieldCheck size={12} />}
            label={t('contracts.catalog.summary.approved', 'Approved')}
            count={summary.approvedCount}
            active={lifecycleParam === 'Approved'}
            onClick={() => toggleLifecycle('Approved')}
          />
          <FilterChip
            icon={<Lock size={12} />}
            label={t('contracts.catalog.summary.locked', 'Locked')}
            count={summary.lockedCount}
            active={lifecycleParam === 'Locked'}
            onClick={() => toggleLifecycle('Locked')}
          />
          <FilterChip
            icon={<AlertTriangle size={12} />}
            label={t('contracts.catalog.summary.deprecated', 'Deprecated')}
            count={summary.deprecatedCount}
            active={lifecycleParam === 'Deprecated'}
            onClick={() => toggleLifecycle('Deprecated')}
          />
          <FilterChip
            icon={<ListTree size={12} />}
            label={t('contracts.catalog.summary.distinct', 'Distinct')}
            count={summary.distinctContracts}
          />
        </div>
      )}

      {/* Superfície de descoberta: pesquisa + facetas + Tabela|Cartões */}
      {listQuery.isError ? (
        <PageErrorState
          variant="compact"
          message={t('contracts.catalog.states.errorDescription', 'An error occurred while loading the contract catalog.')}
          onRetry={() => listQuery.refetch()}
        />
      ) : (
        <ContractBrowseSurface
          items={catalogItems}
          loading={listQuery.isLoading}
          onOpen={(item) => navigate(`/contracts/${item.versionId}`)}
          renderTable={(rows) => (
            <CatalogTable items={applySorting(rows, sort)} sort={sort} onSort={setSort} />
          )}
        />
      )}
    </PageContainer>
  );
}

// ── Client-side sorting (aplica-se às linhas já filtradas pela browse surface) ──

function applySorting(items: CatalogItem[], sort: SortConfig): CatalogItem[] {
  const sorted = [...items];
  const { field, direction } = sort;
  const dir = direction === 'asc' ? 1 : -1;

  sorted.sort((a, b) => {
    const va = getSortValue(a, field);
    const vb = getSortValue(b, field);
    if (va < vb) return -1 * dir;
    if (va > vb) return 1 * dir;
    return 0;
  });

  return sorted;
}

function getSortValue(item: CatalogItem, field: SortField): string | number {
  switch (field) {
    case 'name': return item.name.toLowerCase();
    case 'serviceType': return item.catalogServiceType;
    case 'semVer': return item.semVer;
    case 'criticality': return item.criticality;
    case 'lifecycleState': return item.lifecycleState;
    case 'updatedAt': return item.updatedAt;
    default: return '';
  }
}
