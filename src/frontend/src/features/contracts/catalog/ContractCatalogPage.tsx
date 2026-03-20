import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Layers, FileCheck, Clock, ShieldCheck, Lock, AlertTriangle, ListTree } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { FilterChip } from '../../../components/FilterChip';
import { ErrorState } from '../shared/components';
import { PageContainer } from '../../../components/shell';
import { useContractList, useContractsSummary } from '../hooks';
import { CatalogToolbar, CatalogTable, CatalogSkeleton } from './components';
import type { CatalogFilters, SortConfig, SortField, CatalogItem } from './types';
import { EMPTY_FILTERS, toCatalogItem, extractFilterOptions } from './types';

/**
 * Catálogo governado de contratos — listagem enterprise com filtros avançados,
 * sorting, badges semânticos e menu de acções por linha.
 *
 * Suporta REST API, SOAP, Event API, Kafka Producer/Consumer e BackgroundService.
 * Dados enriquecidos (domain, team, owner, criticality) vêm do backend real.
 */
export function ContractCatalogPage() {
  const { t } = useTranslation();

  // ── State ───────────────────────────────────────────────────────────────────
  const [filters, setFilters] = useState<CatalogFilters>(EMPTY_FILTERS);
  const [sort, setSort] = useState<SortConfig>({ field: 'updatedAt', direction: 'desc' });
  const [lifecycleChip, setLifecycleChip] = useState<string>('');

  // ── Data fetching (Phase 2 hooks) ───────────────────────────────────────────
  const listQuery = useContractList({
    searchTerm: filters.search || undefined,
    protocol: filters.protocol || undefined,
    lifecycleState: filters.lifecycle || lifecycleChip || undefined,
  });

  const summaryQuery = useContractsSummary();
  const summary = summaryQuery.data;

  // ── Transform to catalog items & client-side filtering ────────────────────────
  const catalogItems = useMemo(
    () => (listQuery.data?.items ?? []).map(toCatalogItem),
    [listQuery.data],
  );

  const dynamicOptions = useMemo(
    () => extractFilterOptions(catalogItems),
    [catalogItems],
  );

  const filteredItems = useMemo(
    () => applyClientFilters(catalogItems, filters, lifecycleChip),
    [catalogItems, filters, lifecycleChip],
  );

  const sortedItems = useMemo(
    () => applySorting(filteredItems, sort),
    [filteredItems, sort],
  );

  // ── Lifecycle chip toggle ───────────────────────────────────────────────────
  const toggleChip = (state: string) =>
    setLifecycleChip((prev) => (prev === state ? '' : state));

  // ── Render ──────────────────────────────────────────────────────────────────
  return (
    <PageContainer>
      {/* Header */}
      <PageHeader
        title={t('contracts.catalog.title', 'Contract Catalog')}
        subtitle={t('contracts.catalog.subtitle', 'Governed catalog of all contracts, APIs, and service definitions.')}
        actions={
          <Link to="/contracts/new">
            <Button size="sm">
              <Plus size={14} />
              {t('contracts.create.title', 'Create')}
            </Button>
          </Link>
        }
      />

      {/* Summary chips */}
      {summary && (
        <div className="flex items-center gap-2 flex-wrap">
          <FilterChip
            icon={<Layers size={12} />}
            label={t('contracts.catalog.summary.total', 'Total')}
            count={summary.totalVersions}
            active={lifecycleChip === ''}
            onClick={() => setLifecycleChip('')}
          />
          <FilterChip
            icon={<FileCheck size={12} />}
            label={t('contracts.catalog.summary.drafts', 'Drafts')}
            count={summary.draftCount}
            active={lifecycleChip === 'Draft'}
            onClick={() => toggleChip('Draft')}
          />
          <FilterChip
            icon={<Clock size={12} />}
            label={t('contracts.catalog.summary.inReview', 'In Review')}
            count={summary.inReviewCount}
            active={lifecycleChip === 'InReview'}
            onClick={() => toggleChip('InReview')}
          />
          <FilterChip
            icon={<ShieldCheck size={12} />}
            label={t('contracts.catalog.summary.approved', 'Approved')}
            count={summary.approvedCount}
            active={lifecycleChip === 'Approved'}
            onClick={() => toggleChip('Approved')}
          />
          <FilterChip
            icon={<Lock size={12} />}
            label={t('contracts.catalog.summary.locked', 'Locked')}
            count={summary.lockedCount}
            active={lifecycleChip === 'Locked'}
            onClick={() => toggleChip('Locked')}
          />
          <FilterChip
            icon={<AlertTriangle size={12} />}
            label={t('contracts.catalog.summary.deprecated', 'Deprecated')}
            count={summary.deprecatedCount}
            active={lifecycleChip === 'Deprecated'}
            onClick={() => toggleChip('Deprecated')}
          />
          <FilterChip
            icon={<ListTree size={12} />}
            label={t('contracts.catalog.summary.distinct', 'Distinct')}
            count={summary.distinctContracts}
          />
        </div>
      )}

      {/* Toolbar: search + filters */}
      <CatalogToolbar
        filters={filters}
        onChange={setFilters}
        resultCount={sortedItems.length}
        dynamicOptions={dynamicOptions}
      />

      {/* Table */}
      <Card>
        <CardBody className="p-0">
          {listQuery.isLoading && <CatalogSkeleton />}

          {listQuery.isError && (
            <ErrorState
              message={t('contracts.catalog.states.errorDescription', 'An error occurred while loading the contract catalog.')}
              onRetry={() => listQuery.refetch()}
            />
          )}

          {!listQuery.isLoading && !listQuery.isError && sortedItems.length === 0 && (
            <EmptyState
              title={
                catalogItems.length === 0
                  ? t('contracts.catalog.states.empty', 'No contracts in the catalog')
                  : t('contracts.catalog.states.noResults', 'No contracts match your filters')
              }
              description={
                catalogItems.length === 0
                  ? t('contracts.catalog.states.emptyDescription', 'Start by creating your first service contract or importing an existing specification.')
                  : t('contracts.catalog.states.noResultsDescription', 'Try adjusting your search criteria or clearing some filters.')
              }
              action={
                catalogItems.length === 0 ? (
                  <Link to="/contracts/new">
                    <Button variant="secondary" size="sm">
                      <Plus size={12} />
                      {t('contracts.create.title', 'Create')}
                    </Button>
                  </Link>
                ) : undefined
              }
              size="compact"
            />
          )}

          {!listQuery.isLoading && sortedItems.length > 0 && (
            <CatalogTable
              items={sortedItems}
              sort={sort}
              onSort={setSort}
            />
          )}
        </CardBody>
      </Card>
    </PageContainer>
  );
}

// ── Client-side filtering (for real backend fields) ──────────────────────────

function applyClientFilters(
  items: CatalogItem[],
  filters: CatalogFilters,
  lifecycleChip: string,
): CatalogItem[] {
  return items.filter((item) => {
    if (filters.serviceType && item.catalogServiceType !== filters.serviceType) return false;
    if (filters.domain && item.domain !== filters.domain) return false;
    if (filters.owner && item.technicalOwner !== filters.owner) return false;
    if (filters.team && item.team !== filters.team) return false;
    if (filters.approvalState && item.approvalState !== filters.approvalState) return false;
    if (filters.exposure && item.exposure !== filters.exposure) return false;
    if (filters.risk && item.criticality !== filters.risk) return false;

    if (lifecycleChip && item.lifecycleState !== lifecycleChip) return false;

    return true;
  });
}

// ── Client-side sorting ───────────────────────────────────────────────────────

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
