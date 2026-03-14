import { useTranslation } from 'react-i18next';
import { ShieldCheck } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página de AI Policies — governança de acesso, tokens e modelos IA.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function AiPoliciesPage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('aiHub.policiesTitle')}</h1>
        <p className="text-muted mt-1">{t('aiHub.policiesSubtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<ShieldCheck size={24} />}
            title={t('aiHub.policiesEmptyTitle')}
            description={t('aiHub.policiesEmptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
