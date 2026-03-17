import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import {
  TrendingUp,
  TrendingDown,
  Minus,
  Search,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';

/**
 * Página de adoção por módulo.
 *
 * Mostra para cada módulo do produto: percentagem de adoção, número de ações,
 * utilizadores únicos, profundidade de uso e tendência.
 * Permite filtrar por persona e período.
 *
 * @see docs/MODULES-AND-PAGES.md — módulos oficiais do produto
 */

/* ── Dados de demonstração (MVP) ── */

const mockModules = [
  { module: 'Search', moduleName: 'Search', adoptionPercent: 94, totalActions: 4120, uniqueUsers: 210, depthScore: 96.1, trend: 'Stable' as const, topFeatures: ['global_search', 'command_palette', 'filter'] },
  { module: 'SourceOfTruth', moduleName: 'Source of Truth', adoptionPercent: 89, totalActions: 3240, uniqueUsers: 156, depthScore: 92.3, trend: 'Improving' as const, topFeatures: ['query_contracts', 'view_services', 'search_schemas'] },
  { module: 'ContractStudio', moduleName: 'Contract Studio', adoptionPercent: 76, totalActions: 2810, uniqueUsers: 134, depthScore: 78.4, trend: 'Improving' as const, topFeatures: ['create_draft', 'edit_contract', 'publish', 'validate'] },
  { module: 'AiAssistant', moduleName: 'AI Assistant', adoptionPercent: 72, totalActions: 1980, uniqueUsers: 112, depthScore: 65.8, trend: 'Improving' as const, topFeatures: ['prompt_submit', 'response_used', 'context_query'] },
  { module: 'ChangeIntelligence', moduleName: 'Change Intelligence', adoptionPercent: 68, totalActions: 2150, uniqueUsers: 98, depthScore: 71.2, trend: 'Stable' as const, topFeatures: ['view_changes', 'blast_radius', 'correlation'] },
  { module: 'Incidents', moduleName: 'Incidents', adoptionPercent: 54, totalActions: 1420, uniqueUsers: 87, depthScore: 58.1, trend: 'Stable' as const, topFeatures: ['investigate', 'mitigation_start', 'mitigation_complete'] },
  { module: 'Reliability', moduleName: 'Reliability', adoptionPercent: 48, totalActions: 1247, uniqueUsers: 62, depthScore: 45.6, trend: 'Declining' as const, topFeatures: ['view_dashboard', 'set_objectives', 'review_sla'] },
  { module: 'Governance', moduleName: 'Governance', adoptionPercent: 41, totalActions: 980, uniqueUsers: 45, depthScore: 52.3, trend: 'Stable' as const, topFeatures: ['view_policies', 'compliance_check', 'evidence_export'] },
  { module: 'Runbooks', moduleName: 'Runbooks', adoptionPercent: 38, totalActions: 540, uniqueUsers: 34, depthScore: 35.7, trend: 'Declining' as const, topFeatures: ['view_runbook', 'execute_step'] },
  { module: 'ExecutiveViews', moduleName: 'Executive Views', adoptionPercent: 35, totalActions: 720, uniqueUsers: 28, depthScore: 88.2, trend: 'Improving' as const, topFeatures: ['overview', 'risk_heatmap', 'maturity', 'benchmarking'] },
  { module: 'FinOps', moduleName: 'FinOps', adoptionPercent: 32, totalActions: 650, uniqueUsers: 24, depthScore: 42.1, trend: 'Stable' as const, topFeatures: ['cost_view', 'waste_analysis', 'efficiency'] },
  { module: 'Automation', moduleName: 'Automation', adoptionPercent: 28, totalActions: 420, uniqueUsers: 22, depthScore: 31.5, trend: 'Improving' as const, topFeatures: ['create_workflow', 'execute', 'schedule'] },
  { module: 'IntegrationHub', moduleName: 'Integration Hub', adoptionPercent: 22, totalActions: 380, uniqueUsers: 18, depthScore: 28.4, trend: 'Stable' as const, topFeatures: ['configure_connector', 'view_execution', 'freshness'] },
  { module: 'DeveloperPortal', moduleName: 'Developer Portal', adoptionPercent: 18, totalActions: 280, uniqueUsers: 14, depthScore: 22.3, trend: 'Stable' as const, topFeatures: ['browse_apis', 'playground', 'subscribe'] },
];

function trendIcon(trend: 'Improving' | 'Stable' | 'Declining') {
  switch (trend) {
    case 'Improving': return <TrendingUp size={14} className="text-emerald-400" />;
    case 'Declining': return <TrendingDown size={14} className="text-red-400" />;
    default: return <Minus size={14} className="text-zinc-400" />;
  }
}

function adoptionColor(percent: number): string {
  if (percent >= 75) return 'bg-emerald-500';
  if (percent >= 50) return 'bg-accent';
  if (percent >= 30) return 'bg-amber-500';
  return 'bg-red-500';
}

export function ModuleAdoptionPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');

  const filtered = mockModules.filter((m) => {
    if (!search) return true;
    const q = search.toLowerCase();
    return m.moduleName.toLowerCase().includes(q) || m.topFeatures.some((f) => f.includes(q));
  });

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-white">{t('analytics.adoption.title')}</h1>
        <p className="text-zinc-400 mt-1">{t('analytics.adoption.subtitle')}</p>
      </div>

      {/* Search */}
      <div className="flex items-center gap-2 mb-6">
        <div className="relative flex-1 max-w-sm">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-zinc-500" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('analytics.adoption.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 rounded-lg bg-zinc-900 border border-zinc-700 text-white placeholder-zinc-500 focus:border-accent/50 focus:outline-none text-sm"
          />
        </div>
      </div>

      {/* Module list */}
      <div className="space-y-3">
        {filtered.map((mod) => (
          <Card key={mod.module}>
            <CardBody>
              <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                {/* Module info */}
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <span className="text-white font-semibold">{mod.moduleName}</span>
                    {trendIcon(mod.trend)}
                    <span className="text-xs text-zinc-500">{t(`analytics.trend.${mod.trend}`)}</span>
                  </div>
                  {/* Adoption bar */}
                  <div className="flex items-center gap-3">
                    <div className="w-48 h-2 rounded-full bg-zinc-800 overflow-hidden">
                      <div
                        className={`h-full rounded-full ${adoptionColor(mod.adoptionPercent)} transition-all`}
                        style={{ width: `${mod.adoptionPercent}%` }}
                      />
                    </div>
                    <span className="text-sm text-white font-medium">{mod.adoptionPercent}%</span>
                  </div>
                </div>

                {/* Stats */}
                <div className="flex items-center gap-6 text-sm">
                  <div className="text-center">
                    <div className="text-white font-medium">{mod.totalActions.toLocaleString()}</div>
                    <div className="text-zinc-500 text-xs">{t('analytics.actions')}</div>
                  </div>
                  <div className="text-center">
                    <div className="text-white font-medium">{mod.uniqueUsers}</div>
                    <div className="text-zinc-500 text-xs">{t('analytics.users')}</div>
                  </div>
                  <div className="text-center">
                    <div className="text-white font-medium">{mod.depthScore.toFixed(1)}</div>
                    <div className="text-zinc-500 text-xs">{t('analytics.adoption.depthScore')}</div>
                  </div>
                </div>
              </div>

              {/* Top features */}
              <div className="mt-3 flex flex-wrap gap-2">
                {mod.topFeatures.map((f) => (
                  <span key={f} className="px-2 py-0.5 rounded-md bg-zinc-800 text-zinc-400 text-xs">
                    {f}
                  </span>
                ))}
              </div>
            </CardBody>
          </Card>
        ))}
      </div>

      {filtered.length === 0 && (
        <div className="text-center py-12 text-zinc-500">{t('analytics.adoption.noResults')}</div>
      )}
    </PageContainer>
  );
}
