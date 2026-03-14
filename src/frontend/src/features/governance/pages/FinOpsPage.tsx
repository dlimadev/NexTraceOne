import { useTranslation } from 'react-i18next';
import { DollarSign } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página de FinOps — otimização de custos contextualizada por serviço, equipa e operação.
 * Parte do módulo Governance do NexTraceOne.
 */
export function FinOpsPage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.finopsTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.finopsSubtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<DollarSign size={24} />}
            title={t('governance.finopsEmptyTitle')}
            description={t('governance.finopsEmptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
