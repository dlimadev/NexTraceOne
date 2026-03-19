import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import { Play, ArrowLeft } from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer } from '../../../components/shell';

/**
 * Página de simulação de Governance Pack.
 * Nesta fase a capability permanece em preview e não expõe resultados simulados
 * como se fossem projeções reais de produção.
 */
export function PackSimulationPage() {
  const { t } = useTranslation();
  const { packId } = useParams<{ packId: string }>();
  const backTo = `/governance/packs/${packId ?? ''}`;

  return (
    <PageContainer>
      <Link
        to={backTo}
        className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4"
      >
        <ArrowLeft size={14} />
        {t('governancePacks.simulation.backToPack')}
      </Link>

      <div className="mb-6">
        <div className="flex items-center gap-3 mb-2">
          <Play size={24} className="text-accent" />
          <h1 className="text-2xl font-bold text-heading">{t('governancePacks.simulation.title')}</h1>
        </div>
        <p className="text-muted mt-1">
          {t('governancePacks.simulation.subtitle', { packName: packId ?? '' })}
        </p>
        <div className="flex items-center gap-2 mt-2">
          <Badge variant="warning">{t('governance.preview.badge')}</Badge>
          <span className="text-xs text-muted">{t('governance.preview.simulationReason')}</span>
        </div>
      </div>

      <EmptyState
        icon={<Play size={24} />}
        title={t('governancePacks.detail.simulationTitle')}
        description={t(
          'governance.preview.simulationUnavailable',
          'Simulation remains available only as a preview placeholder in this release. Real impact projections are not exposed until the workflow is backed by production data.',
        )}
        action={
          <Link
            to={backTo}
            className="inline-flex items-center gap-2 rounded-md border border-accent/30 bg-accent/10 px-4 py-2 text-sm font-medium text-accent hover:bg-accent/15 transition-colors"
          >
            <ArrowLeft size={14} />
            {t('governancePacks.simulation.backToPack')}
          </Link>
        }
      />
    </PageContainer>
  );
}
