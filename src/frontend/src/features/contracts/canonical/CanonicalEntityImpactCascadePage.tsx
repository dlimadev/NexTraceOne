/**
 * CanonicalEntityImpactCascadePage — análise em cascata de impacto de entidades canónicas.
 * Mostra quantos contratos são afectados directa e indirectamente quando uma entidade canónica muda.
 * Pilar: Contract Governance + Change Intelligence.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { GitBranch, AlertTriangle, CheckCircle2, ChevronRight, ChevronDown } from 'lucide-react';
import { contractsApi } from '../api/contracts';

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
  None: 'text-green-400',
  Low: 'text-green-400',
  Medium: 'text-yellow-400',
  High: 'text-orange-400',
  Critical: 'text-red-400',
};

const RISK_BG: Record<string, string> = {
  None: 'bg-green-400/10 border-green-400/25',
  Low: 'bg-green-400/10 border-green-400/25',
  Medium: 'bg-yellow-400/10 border-yellow-400/25',
  High: 'bg-orange-400/10 border-orange-400/25',
  Critical: 'bg-red-400/10 border-red-400/25',
};

function CascadeNodeTree({ node, depth = 0 }: { node: CascadeNode; depth?: number }) {
  const { t } = useTranslation();
  const [expanded, setExpanded] = useState(depth === 0);
  const hasChildren = node.children.length > 0;

  return (
    <div className="ml-4 border-l border-slate-700 pl-3">
      <button
        onClick={() => setExpanded((v) => !v)}
        className="flex items-center gap-2 py-1.5 w-full text-left hover:text-white transition-colors text-slate-300"
        aria-label={t('phase4.impactCascade.entityName')}
      >
        {hasChildren ? (
          expanded ? <ChevronDown size={14} className="text-slate-500" /> : <ChevronRight size={14} className="text-slate-500" />
        ) : (
          <span className="w-3.5" />
        )}
        <GitBranch size={14} className="text-blue-400 shrink-0" />
        <span className="font-medium text-sm">{node.entityName}</span>
        <span className="text-xs text-slate-500 ml-1">
          {t('phase4.impactCascade.depthLabel', { depth: node.depth })}
        </span>
        <span className="ml-auto text-xs bg-slate-700 rounded px-1.5 py-0.5">
          {node.affectedContractIds.length} {t('phase4.impactCascade.affectedContracts')}
        </span>
      </button>
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

  const riskColor = data ? (RISK_COLORS[data.riskLevel] ?? 'text-slate-400') : 'text-slate-400';
  const riskBg = data ? (RISK_BG[data.riskLevel] ?? 'bg-slate-700/10 border-slate-700/25') : '';

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold text-white flex items-center gap-2">
          <GitBranch size={24} className="text-blue-400" />
          {t('phase4.impactCascade.title', 'Canonical Entity Impact Cascade')}
        </h1>
        <p className="text-slate-400 text-sm mt-1">
          {t('phase4.impactCascade.subtitle', 'Multi-level cascade analysis of canonical entity changes')}
        </p>
      </div>

      {/* Controls */}
      <div className="bg-slate-800 rounded-lg border border-slate-700 p-4 space-y-4">
        <div className="flex gap-3 flex-wrap">
          <div className="flex-1 min-w-[260px]">
            <label className="block text-xs text-slate-400 mb-1 uppercase tracking-wide">
              {t('phase4.impactCascade.entityName', 'Canonical Entity ID')}
            </label>
            <input
              type="text"
              value={entityId}
              onChange={(e) => setEntityId(e.target.value)}
              placeholder="e.g. 3fa85f64-5717-4562-b3fc-2c963f66afa6"
              className="w-full px-3 py-2 rounded-lg bg-slate-700 border border-slate-600 text-white text-sm placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              aria-label={t('phase4.impactCascade.entityName')}
            />
          </div>
          <div className="w-36">
            <label className="block text-xs text-slate-400 mb-1 uppercase tracking-wide">
              {t('phase4.impactCascade.maxDepth', 'Max Depth')}
            </label>
            <select
              value={maxDepth}
              onChange={(e) => setMaxDepth(Number(e.target.value))}
              className="w-full px-3 py-2 rounded-lg bg-slate-700 border border-slate-600 text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              aria-label={t('phase4.impactCascade.maxDepth')}
            >
              <option value={1}>1</option>
              <option value={2}>2</option>
              <option value={3}>3</option>
            </select>
          </div>
          <div className="flex items-end">
            <button
              onClick={handleAnalyze}
              disabled={!entityId.trim() || isLoading}
              className="px-4 py-2 text-sm font-medium rounded-lg bg-blue-600 text-white hover:bg-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isLoading ? '...' : t('phase4.impactCascade.analyze', 'Analyze Cascade')}
            </button>
          </div>
        </div>
      </div>

      {/* Error */}
      {isError && (
        <div className="bg-red-500/10 border border-red-500/25 rounded-lg p-4 flex items-center gap-2 text-red-400">
          <AlertTriangle size={16} />
          <span className="text-sm">Failed to load cascade analysis. Please verify the entity ID.</span>
          <button onClick={() => refetch()} className="ml-auto text-xs underline">Retry</button>
        </div>
      )}

      {/* Results */}
      {data && (
        <div className="space-y-4">
          {/* Summary cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <div className="bg-slate-800 rounded-lg border border-slate-700 p-4">
              <div className="text-xs text-slate-400 uppercase tracking-wide mb-1">
                {t('phase4.impactCascade.totalContracts', 'Total Contracts Affected')}
              </div>
              <div className="text-2xl font-semibold text-white tabular-nums">
                {data.totalContractsAffected}
              </div>
            </div>
            <div className="bg-slate-800 rounded-lg border border-slate-700 p-4">
              <div className="text-xs text-slate-400 uppercase tracking-wide mb-1">
                {t('phase4.impactCascade.uniqueEntities', 'Unique Entities in Cascade')}
              </div>
              <div className="text-2xl font-semibold text-white tabular-nums">
                {data.totalUniqueEntitiesInCascade}
              </div>
            </div>
            <div className={`rounded-lg border p-4 ${riskBg}`}>
              <div className="text-xs text-slate-400 uppercase tracking-wide mb-1">
                {t('phase4.impactCascade.riskLevel', 'Risk Level')}
              </div>
              <div className={`text-2xl font-semibold tabular-nums ${riskColor}`}>
                {data.riskLevel}
              </div>
            </div>
            <div className="bg-slate-800 rounded-lg border border-slate-700 p-4">
              <div className="text-xs text-slate-400 uppercase tracking-wide mb-1">
                {t('phase4.impactCascade.maxDepth', 'Max Depth Reached')}
              </div>
              <div className="text-2xl font-semibold text-white tabular-nums">
                {data.maxDepthReached}
              </div>
            </div>
          </div>

          {/* Root entity info */}
          <div className="bg-slate-800 rounded-lg border border-slate-700 p-4">
            <div className="flex items-center gap-2 mb-3">
              <CheckCircle2 size={16} className="text-blue-400" />
              <h2 className="text-sm font-medium text-white">
                {data.rootEntityName}
              </h2>
            </div>

            {/* Tree */}
            {data.cascadeNodes.length === 0 ? (
              <p className="text-slate-400 text-sm">
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
    </div>
  );
}
