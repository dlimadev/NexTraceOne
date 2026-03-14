import { useTranslation } from 'react-i18next';
import { ShieldAlert } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página de Risk Center — análise de risco operacional contextualizado por serviço e mudança.
 * Parte do módulo Governance do NexTraceOne.
 */
export function RiskCenterPage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.riskTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.riskSubtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<ShieldAlert size={24} />}
            title={t('governance.riskEmptyTitle')}
            description={t('governance.riskEmptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
