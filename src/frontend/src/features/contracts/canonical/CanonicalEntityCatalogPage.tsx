import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Database,
  Plus,
  Tag,
  ChevronDown,
  ChevronRight,
  ExternalLink,
  ArrowUpRight,
  BookOpen,
  GitBranch,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { LoadingState, ErrorState } from '../shared/components/StateIndicators';
import { Button, IconButton, SearchInput, Select } from '../../../shared/ui';
import { stateToVariant } from '../lib/contractVariants';
import { useCanonicalEntities, useCanonicalEntityUsages } from '../hooks';
import type { CanonicalEntity, CanonicalEntityState } from '../types';

/** Cores para cards de stat — uso restrito a contêineres de sumário, não a badges de estado. */
const STATE_CARD_COLORS: Record<CanonicalEntityState, string> = {
  Draft: 'bg-muted/15 text-muted border border-muted/25',
  Published: 'bg-mint/15 text-mint border border-mint/25',
  Deprecated: 'bg-warning/15 text-warning border border-warning/25',
  Retired: 'bg-danger/15 text-danger border border-danger/25',
};

/**
 * Página de catálogo de entidades Canonical.
 * Permite pesquisa, filtros, detalhe e gestão de entidades padronizadas reutilizáveis.
 */
export function CanonicalEntityCatalogPage() {
  const { t } = useTranslation();
  const [searchTerm, setSearchTerm] = useState('');
  const [stateFilter, setStateFilter] = useState<CanonicalEntityState | ''>('');
  const [domainFilter, setDomainFilter] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const entitiesQuery = useCanonicalEntities({
    searchTerm: searchTerm || undefined,
    state: stateFilter || undefined,
    domain: domainFilter || undefined,
  });

  const entities = entitiesQuery.data?.items ?? [];
  const total = entitiesQuery.data?.total ?? 0;

  if (entitiesQuery.isLoading) return <PageContainer><LoadingState /></PageContainer>;
  if (entitiesQuery.isError) return <PageContainer><ErrorState onRetry={() => entitiesQuery.refetch()} /></PageContainer>;

  const domains = [...new Set(entities.map((e) => e.domain).filter(Boolean))];

  return (
    <PageContainer>
      <PageHeader
        title={t('contracts.canonical.catalog.title', 'Canonical Entities')}
        subtitle={t('contracts.canonical.catalog.subtitle', 'Standardized, reusable schemas and models for contract governance.')}
        actions={
          <Button variant="primary" size="sm" icon={<Plus size={14} />}>
            {t('contracts.canonical.catalog.create', 'Add Entity')}
          </Button>
        }
      />

      {/* Search & filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <SearchInput
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          placeholder={t('contracts.canonical.catalog.searchPlaceholder', 'Search canonical entities...')}
          className="flex-1 min-w-[240px]"
          size="sm"
        />

        <Select
          value={stateFilter}
          onChange={(e) => setStateFilter(e.target.value as CanonicalEntityState | '')}
          options={[
            { value: '', label: t('contracts.canonical.catalog.allStates', 'All States') },
            ...(['Draft', 'Published', 'Deprecated', 'Retired'] as const).map((s) => ({ value: s, label: s })),
          ]}
          size="sm"
        />

        {domains.length > 0 && (
          <Select
            value={domainFilter}
            onChange={(e) => setDomainFilter(e.target.value)}
            options={[
              { value: '', label: t('contracts.canonical.catalog.allDomains', 'All Domains') },
              ...domains.map((d) => ({ value: d, label: d })),
            ]}
            size="sm"
          />
        )}

        <span className="text-xs text-muted">
          {t('contracts.canonical.catalog.results', '{{count}} entities', { count: total })}
        </span>
      </div>

      {/* Stats */}
      <StatsGrid columns={4}>
        {(['Draft', 'Published', 'Deprecated', 'Retired'] as const).map((state) => (
          <div key={state} className={`rounded-lg border p-4 ${STATE_CARD_COLORS[state]}`}>
            <p className="text-xs opacity-70">{state}</p>
            <p className="text-2xl font-bold mt-1">{entities.filter((e) => e.state === state).length}</p>
          </div>
        ))}
      </StatsGrid>

      {/* Entity list */}
      {entities.length === 0 && !entitiesQuery.isLoading && (
        <EmptyState
          icon={<Database size={24} aria-hidden="true" />}
          title={t('contracts.canonical.catalog.emptyTitle', 'No canonical entities')}
          description={t('contracts.canonical.catalog.emptyDescription', 'Create canonical entities to standardize schemas across contracts.')}
        />
      )}

      <div className="space-y-3">
        {entities.map((entity) => (
          <CanonicalEntityCard
            key={entity.id}
            entity={entity}
            isExpanded={expandedId === entity.id}
            onToggle={() => setExpandedId(expandedId === entity.id ? null : entity.id)}
          />
        ))}
      </div>
    </PageContainer>
  );
}

function CanonicalEntityCard({
  entity,
  isExpanded,
  onToggle,
}: {
  entity: CanonicalEntity;
  isExpanded: boolean;
  onToggle: () => void;
}) {
  const { t } = useTranslation();
  const usagesQuery = useCanonicalEntityUsages(isExpanded ? entity.id : undefined);
  const usages = usagesQuery.data ?? [];

  return (
    <Card>
      <CardBody>
        <div className="flex items-center gap-3">
          <IconButton
            onClick={onToggle}
            icon={isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
            label={t('contracts.canonical.catalog.toggleDetail', 'Toggle detail')}
            variant="ghost"
            size="sm"
          />

          <Database size={16} className="text-accent" />

          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <p className="text-sm font-medium text-heading truncate">{entity.name}</p>
              <span className="text-[10px] text-muted">v{entity.version}</span>
              <Badge variant={stateToVariant(entity.state)} size="xs">
                {entity.state}
              </Badge>
            </div>
            <p className="text-xs text-muted truncate">{entity.description}</p>
          </div>

          {/* Metadata */}
          <div className="flex items-center gap-2">
            {entity.domain && (
              <span className="px-2 py-0.5 text-[10px] rounded-full bg-accent/10 text-accent border border-accent/20">
                {entity.domain}
              </span>
            )}
            {entity.category && (
              <span className="px-2 py-0.5 text-[10px] rounded-full bg-accent/10 text-accent border border-accent/20">
                {entity.category}
              </span>
            )}
            <span className="text-xs text-muted flex items-center gap-1">
              <ArrowUpRight size={10} />
              {entity.knownUsageCount}
            </span>
          </div>
        </div>

        {/* Expanded detail */}
        {isExpanded && (
          <div className="mt-4 pt-4 border-t border-edge/10 space-y-4">
            <div className="grid grid-cols-3 gap-4 text-xs">
              <div>
                <p className="text-muted mb-1">{t('contracts.canonical.catalog.owner', 'Owner')}</p>
                <p className="text-heading">{entity.owner}</p>
              </div>
              <div>
                <p className="text-muted mb-1">{t('contracts.canonical.catalog.criticality', 'Criticality')}</p>
                <p className="text-heading">{entity.criticality || '—'}</p>
              </div>
              <div>
                <p className="text-muted mb-1">{t('contracts.canonical.catalog.reusePolicy', 'Reuse Policy')}</p>
                <p className="text-heading">{entity.reusePolicy || '—'}</p>
              </div>
              <div>
                <p className="text-muted mb-1">{t('contracts.canonical.catalog.format', 'Format')}</p>
                <p className="text-heading">{entity.schemaFormat}</p>
              </div>
              <div>
                <p className="text-muted mb-1">{t('contracts.canonical.catalog.usages', 'Known Usages')}</p>
                <p className="text-heading">{entity.knownUsageCount}</p>
              </div>
              <div>
                <p className="text-muted mb-1">{t('contracts.canonical.catalog.updated', 'Updated')}</p>
                <p className="text-heading">{new Date(entity.updatedAt).toLocaleDateString()}</p>
              </div>
            </div>

            <div>
              <Link
                to={`/contracts/canonical/impact-cascade?entityId=${entity.id}`}
                className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
              >
                <GitBranch size={12} />
                {t('contracts.canonical.catalog.impactCascade', 'Impact cascade')}
              </Link>
            </div>

            {/* Tags & Aliases */}
            {((entity.tags?.length ?? 0) > 0 || (entity.aliases?.length ?? 0) > 0) && (
              <div className="flex flex-wrap gap-2">
                {(entity.tags ?? []).map((tag) => (
                  <span key={tag} className="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] rounded-full bg-accent/10 text-accent border border-accent/20">
                    <Tag size={8} />
                    {tag}
                  </span>
                ))}
                {(entity.aliases ?? []).map((alias) => (
                  <span key={alias} className="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] rounded-full bg-muted/15 text-muted border border-muted/25">
                    <BookOpen size={8} />
                    {alias}
                  </span>
                ))}
              </div>
            )}

            {/* Schema preview */}
            <div>
              <p className="text-xs text-muted mb-1">{t('contracts.canonical.catalog.schema', 'Schema Content')}</p>
              <pre className="p-3 rounded-lg bg-panel/50 border border-edge/10 text-[11px] text-heading font-mono overflow-x-auto max-h-[200px]">
                {(entity.schemaContent ?? '').slice(0, 800)}
                {(entity.schemaContent?.length ?? 0) > 800 && '...'}
              </pre>
            </div>

            {/* Usages */}
            {usages.length > 0 && (
              <div>
                <p className="text-xs text-muted mb-2">
                  {t('contracts.canonical.catalog.usedBy', 'Used by')}
                  <span className="ml-1 text-heading">({usages.length})</span>
                </p>
                <div className="space-y-1">
                  {usages.map((u, idx) => (
                    <div
                      key={idx}
                      className="flex items-center justify-between px-3 py-2 text-xs rounded-lg bg-elevated/30 border border-edge/10"
                    >
                      <div className="flex items-center gap-2">
                        <ExternalLink size={10} className="text-accent" />
                        <span className="text-heading">{u.apiAssetId}</span>
                        <span className="text-muted">· {u.referencePath}</span>
                      </div>
                      <Badge variant={u.isConformant ? 'success' : 'danger'} size="xs">
                        {u.isConformant
                          ? t('contracts.canonical.catalog.conformant', 'Conformant')
                          : t('contracts.canonical.catalog.nonConformant', 'Non-conformant')}
                      </Badge>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </CardBody>
    </Card>
  );
}
