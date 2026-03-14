import { useTranslation } from 'react-i18next';
import { Database } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página do Model Registry — registo e gestão de modelos IA internos e externos.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function ModelRegistryPage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('aiHub.modelsTitle')}</h1>
        <p className="text-muted mt-1">{t('aiHub.modelsSubtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<Database size={24} />}
            title={t('aiHub.modelsEmptyTitle')}
            description={t('aiHub.modelsEmptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
