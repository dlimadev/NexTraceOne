import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  TrendingUp,
  TrendingDown,
  Minus,
  Users,
  CheckCircle,
  AlertCircle,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { productAnalyticsApi } from '../api/productAnalyticsApi';
import type { PersonaUsageProfileDto } from '../../../types';

/**
 * Página de uso por persona.
 *
 * Mostra perfil de uso para cada persona oficial do NexTraceOne:
 * módulos usados, ações principais, profundidade de adoção,
 * pontos de fricção comuns e marcos de valor atingidos.
 * Alimentada pelo endpoint real /product-analytics/adoption/personas.
 *
 * @see docs/PERSONA-MATRIX.md — definição oficial das personas
 */

function trendIcon(depth: number) {
  if (depth >= 80) return <TrendingUp size={14} className="text-emerald-400" />;
  if (depth >= 60) return <Minus size={14} className="text-muted" />;
  return <TrendingDown size={14} className="text-amber-400" />;
}

function depthColor(depth: number): string {
  if (depth >= 80) return 'text-emerald-400';
  if (depth >= 60) return 'text-accent';
  if (depth >= 40) return 'text-amber-400';
  return 'text-red-400';
}

export function PersonaUsagePage() {
  const { t } = useTranslation();
  const [selectedPersona, setSelectedPersona] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-persona-usage'],
    queryFn: () => productAnalyticsApi.getPersonaUsage({ range: 'last_30d' }),
    staleTime: 15_000,
  });

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState message={t('common.loading')} />
      </PageContainer>
    );
  }

  if (isError || !data) {
    return (
      <PageContainer>
        <PageErrorState
          action={
            <button
              type="button"
              onClick={() => refetch()}
              className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50"
            >
              {t('common.retry')}
            </button>
          }
        />
      </PageContainer>
    );
  }

  const allProfiles = data.profiles;
  const profiles: PersonaUsageProfileDto[] = selectedPersona
    ? allProfiles.filter((p) => p.persona === selectedPersona)
    : allProfiles;

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.persona.title')}
        subtitle={t('analytics.persona.subtitle')}
      />

      {/* Persona filter */}
      <div className="flex flex-wrap gap-2 mb-6">
        <button
          type="button"
          onClick={() => setSelectedPersona(null)}
          className={`px-3 py-1.5 rounded-lg text-sm transition ${!selectedPersona ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-elevated text-muted border border-edge hover:border-edge-strong'}`}
        >
          {t('analytics.persona.all')}
        </button>
        {allProfiles.map((p) => (
          <button
            type="button"
            key={p.persona}
            onClick={() => setSelectedPersona(p.persona)}
            className={`px-3 py-1.5 rounded-lg text-sm transition ${selectedPersona === p.persona ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-elevated text-muted border border-edge hover:border-edge-strong'}`}
          >
            {t(`analytics.persona.role.${p.persona}`, { defaultValue: p.persona })}
          </button>
        ))}
      </div>

      {profiles.length === 0 ? (
        <div className="text-center py-12 text-faded">{t('common.noData')}</div>
      ) : (
        /* Persona cards */
        <div className="space-y-4">
          {profiles.map((p) => (
            <Card key={p.persona}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <Users size={18} className="text-accent" />
                    <span className="font-semibold text-heading">{t(`analytics.persona.role.${p.persona}`, { defaultValue: p.persona })}</span>
                    {trendIcon(p.adoptionDepth)}
                  </div>
                  <div className="flex items-center gap-4 text-sm">
                    <span className="text-muted">{p.activeUsers} {t('analytics.users')}</span>
                    <span className="text-muted">{p.totalActions.toLocaleString()} {t('analytics.actions')}</span>
                    <span className={`font-medium ${depthColor(p.adoptionDepth)}`}>{p.adoptionDepth}% {t('analytics.persona.depth')}</span>
                  </div>
                </div>
              </CardHeader>
              <CardBody>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                  {/* Top modules */}
                  <div>
                    <h4 className="text-xs text-faded uppercase tracking-wide mb-2">{t('analytics.persona.topModules')}</h4>
                    <div className="space-y-2">
                      {p.topModules.map((mod) => (
                        <div key={mod.module} className="flex items-center justify-between">
                          <span className="text-sm text-body">{mod.module}</span>
                          <div className="flex items-center gap-2">
                            <div className="w-16 h-1.5 rounded-full bg-elevated overflow-hidden">
                              <div
                                className="h-full rounded-full bg-accent transition-all"
                                style={{ width: `${mod.adoptionPercent}%` }}
                              />
                            </div>
                            <span className="text-xs text-faded">{mod.adoptionPercent}%</span>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Milestones reached */}
                  <div>
                    <h4 className="text-xs text-faded uppercase tracking-wide mb-2">{t('analytics.persona.milestonesReached')}</h4>
                    <div className="space-y-1">
                      {p.milestonesReached.map((m) => (
                        <div key={m} className="flex items-center gap-2">
                          <CheckCircle size={14} className="text-emerald-400" />
                          <span className="text-sm text-body">{t(`analytics.milestone.${m}`, { defaultValue: m.replace(/([A-Z])/g, ' $1').trim() })}</span>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Friction points */}
                  <div>
                    <h4 className="text-xs text-faded uppercase tracking-wide mb-2">{t('analytics.persona.frictionPoints')}</h4>
                    <div className="space-y-1">
                      {p.commonFrictionPoints.map((f) => (
                        <div key={f} className="flex items-center gap-2">
                          <AlertCircle size={14} className="text-amber-400" />
                          <span className="text-sm text-body">{f.replace(/_/g, ' ')}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}
