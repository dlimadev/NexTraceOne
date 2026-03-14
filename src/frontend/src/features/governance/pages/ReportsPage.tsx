import { useTranslation } from 'react-i18next';
import { BarChart3 } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página de Reports — relatórios segmentados por persona e contexto operacional.
 * Parte do módulo Governance do NexTraceOne.
 */
export function ReportsPage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.reportsTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.reportsSubtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<BarChart3 size={24} />}
            title={t('governance.reportsEmptyTitle')}
            description={t('governance.reportsEmptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
