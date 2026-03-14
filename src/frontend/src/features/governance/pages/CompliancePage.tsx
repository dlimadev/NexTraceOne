import { useTranslation } from 'react-i18next';
import { Scale } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página de Compliance — conformidade regulatória e políticas de governança.
 * Parte do módulo Governance do NexTraceOne.
 */
export function CompliancePage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.complianceTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.complianceSubtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<Scale size={24} />}
            title={t('governance.complianceEmptyTitle')}
            description={t('governance.complianceEmptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
