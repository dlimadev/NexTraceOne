import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página de Incidentes — correlação de incidentes com serviços, mudanças e contratos.
 * Primeiro passo para o módulo Operations do NexTraceOne.
 */
export function IncidentsPage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('incidents.title')}</h1>
        <p className="text-muted mt-1">{t('incidents.subtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<AlertTriangle size={24} />}
            title={t('incidents.emptyTitle')}
            description={t('incidents.emptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
