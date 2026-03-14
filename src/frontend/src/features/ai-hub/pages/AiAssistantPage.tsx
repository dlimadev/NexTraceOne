import { useTranslation } from 'react-i18next';
import { Bot } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página do AI Assistant — assistente contextualizado com serviços, contratos e incidentes.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function AiAssistantPage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('aiHub.assistantTitle')}</h1>
        <p className="text-muted mt-1">{t('aiHub.assistantSubtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<Bot size={24} />}
            title={t('aiHub.assistantEmptyTitle')}
            description={t('aiHub.assistantEmptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
