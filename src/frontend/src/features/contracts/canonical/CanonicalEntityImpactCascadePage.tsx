/**
 * CanonicalEntityImpactCascadePage — análise em cascata de impacto de entidades canónicas.
 * Mostra quantos contratos são afectados directa e indirectamente quando uma entidade canónica muda.
 * Pilar: Contract Governance + Change Intelligence.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { GitBranch, AlertTriangle, CheckCircle2, ChevronRight, ChevronDown, Loader2 } from 'lucide-react';
import { contractsApi } from '../api/contracts';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, Select } from '../../../shared/ui';

type CascadeNode = {
  entityName: string;
  depth: number;
  affectedContractIds: string[];
  children: CascadeNode[];
};

type CascadeResponse = {
  rootEntityId: string;
  rootEntityName: string;
  totalContractsAffected: number;
  totalUniqueEntitiesInCascade: number;
  cascadeNodes: CascadeNode[];
  maxDepthReached: number;
  riskLevel: string;
};

const RISK_COLORS: Record<string, string> = {
  None: 'text-success',
  Low: 'text-success',
  Medium: 'text-warning',
  High: 'text-warning',
  Critical: 'text-critical',
};

const RISK_BG: Record<string, string> = {
  None: 'bg-success/10 border-success/25',
  Low: 'bg-success/10 border-success/25',
  Medium: 'bg-warning/10 border-warning/25',
  High: 'bg-warning/10 border-warning/25',
  Critical: 'bg-critical/10 border-critical/25',
};

function CascadeNodeTree({ node, depth = 0 }: { node: CascadeNode; depth?: number }) {
  const { t } = useTranslation();
  const [expanded, setExpanded] = useState(depth === 0);
  const hasChildren = node.children.length > 0;

  return (
    <div className="ml-4 border-l border-edge pl-3">
      {/* Controlo 1: toggle de expansão → DS Button ghost */}
      <Button
        variant="ghost"
        onClick={() => setExpanded((v) => !v)}
        aria-label={t('phase4.impactCascade.entityName')}
        className="w-full justify-start gap-2 py-1.5 px-0 h-auto font-normal text-body hover:text-heading"
      >
        {hasChildren ? (
          expanded ? <ChevronDown size={14} className="text-muted" /> : <ChevronRight size={14} className="text-muted" />
        ) : (
          <span className="w-3.5" />
        )}
        <GitBranch size={14} className="text-accent shrink-0" />
        <span className="font-medium text-sm">{node.entityName}</span>
        <span className="text-xs text-muted ml-1">
          {t('phase4.impactCascade.depthLabel', { depth: node.depth })}
        </span>
        <span className="ml-auto text-xs bg-elevated rounded px-1.5 py-0.5">
          {node.affectedContractIds.length} {t('phase4.impactCascade.affectedContracts')}
        </span>
      </Button>
      {expanded && hasChildren && (
        <div>
          {node.children.map((child, i) => (
            <CascadeNodeTree key={`${child.entityName}-${i}`} node={child} depth={depth + 1} />
          ))}
        </div>
      )}
    </div>
  );
}

/**
 * Página de visualização em cascata do impacto de entidades canónicas.
 */
export function CanonicalEntityImpactCascadePage() {
  const { t } = useTranslation();
  const [entityId, setEntityId] = useState('');
  const [maxDepth, setMaxDepth] = useState(2);
  const [submittedId, setSubmittedId] = useState('');
  const [submittedDepth, setSubmittedDepth] = useState(2);

  const { data, isLoading, isError, refetch } = useQuery<CascadeResponse>({
    queryKey: ['canonical-impact-cascade', submittedId, submittedDepth],
    queryFn: () => contractsApi.getCanonicalEntityImpactCascade(submittedId, submittedDepth) as Promise<CascadeResponse>,
    enabled: !!submittedId,
    staleTime: 30_000,
  });

  const handleAnalyze = () => {
    if (!entityId.trim()) return;
    setSubmittedId(entityId.trim());
    setSubmittedDepth(maxDepth);
  };

  const riskColor = data ? (RISK_COLORS[data.riskLevel] ?? 'text-muted') : 'text-muted';
  const riskBg = data ? (RISK_BG[data.riskLevel] ?? 'bg-elevated/10 border-edge/25') : '';

  return (
    <PageContainer>
      <PageHeader
        title={t('phase4.impactCascade.title', 'Canonical Entity Impact Cascade')}
        subtitle={t('phase4.impactCascade.subtitle', 'Multi-level cascade analysis of canonical entity changes')}
        icon={<GitBranch />}
      />

      {/* Controls */}
      <div className="bg-elevated rounded-lg border border-edge p-4 space-y-4">
        <div className="flex gap-3 flex-wrap">
          {/* Controlo 2: entity ID → DS TextField */}
          <div className="flex-1 min-w-[260px]">
            <TextField
              label={t('phase4.impactCascade.entityName', 'Canonical Entity ID')}
              type="text"
              value={entityId}
              onChange={(e) => setEntityId(e.target.value)}
              placeholder={t('phase4.impactCascade.entityIdPlaceholder', 'Search or enter canonical entity name')}
              size="sm"
            />
          </div>
          {/* Controlo 3: max depth → DS Select */}
          <div className="w-36">
            <Select
              label={t('phase4.impactCascade.maxDepth', 'Max Depth')}
              value={String(maxDepth)}
              onChange={(e) => setMaxDepth(Number(e.target.value))}
              options={[
                { value: '1', label: '1' },
                { value: '2', label: '2' },
                { value: '3', label: '3' },
              ]}
              size="sm"
            />
          </div>
          {/* Controlo 4: analyze → DS Button primary */}
          <div className="flex items-end">
            <Button
              variant="primary"
              onClick={handleAnalyze}
              disabled={!entityId.trim()}
              loading={isLoading}
            >
              {t('phase4.impactCascade.analyze', 'Analyze Cascade')}
            </Button>
          </div>
        </div>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <Loader2 size={24} className="animate-spin text-accent mr-2" />
          <span className="text-muted text-sm">{t('common.loading', 'Loading...')}</span>
        </div>
      )}

      {/* Error */}
      {isError && (
        <div className="bg-critical/10 border border-critical/25 rounded-lg p-4 flex items-center gap-2 text-critical">
          <AlertTriangle size={16} />
          <span className="text-sm">{t('phase4.impactCascade.loadError', 'Failed to load cascade analysis. Please verify the entity ID.')}</span>
          {/* Controlo 5: retry → DS Button ghost */}
          <Button
            variant="ghost"
            onClick={() => refetch()}
            className="ml-auto text-xs underline"
          >
            {t('common.retry', 'Retry')}
          </Button>
        </div>
      )}

      {/* Results */}
      {data && (
        <div className="space-y-4">
          {/* Summary cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <div className="bg-elevated rounded-lg border border-edge p-4">
              <div className="text-xs text-muted uppercase tracking-wide mb-1">
                {t('phase4.impactCascade.totalContracts', 'Total Contracts Affected')}
              </div>
              <div className="text-2xl font-semibold text-heading tabular-nums">
                {data.totalContractsAffected}
              </div>
            </div>
            <div className="bg-elevated rounded-lg border border-edge p-4">
              <div className="text-xs text-muted uppercase tracking-wide mb-1">
                {t('phase4.impactCascade.uniqueEntities', 'Unique Entities in Cascade')}
              </div>
              <div className="text-2xl font-semibold text-heading tabular-nums">
                {data.totalUniqueEntitiesInCascade}
              </div>
            </div>
            <div className={`rounded-lg border p-4 ${riskBg}`}>
              <div className="text-xs text-muted uppercase tracking-wide mb-1">
                {t('phase4.impactCascade.riskLevel', 'Risk Level')}
              </div>
              <div className={`text-2xl font-semibold tabular-nums ${riskColor}`}>
                {data.riskLevel}
              </div>
            </div>
            <div className="bg-elevated rounded-lg border border-edge p-4">
              <div className="text-xs text-muted uppercase tracking-wide mb-1">
                {t('phase4.impactCascade.maxDepth', 'Max Depth Reached')}
              </div>
              <div className="text-2xl font-semibold text-heading tabular-nums">
                {data.maxDepthReached}
              </div>
            </div>
          </div>

          {/* Root entity info */}
          <div className="bg-elevated rounded-lg border border-edge p-4">
            <div className="flex items-center gap-2 mb-3">
              <CheckCircle2 size={16} className="text-accent" />
              <h2 className="text-sm font-medium text-heading">
                {data.rootEntityName}
              </h2>
            </div>

            {/* Tree */}
            {data.cascadeNodes.length === 0 ? (
              <p className="text-muted text-sm">
                {t('phase4.impactCascade.noChildren', 'No cascading impact')}
              </p>
            ) : (
              <div className="space-y-1">
                {data.cascadeNodes.map((node, i) => (
                  <CascadeNodeTree key={`${node.entityName}-${i}`} node={node} depth={0} />
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </PageContainer>
  );
}
