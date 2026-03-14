import { useTranslation } from 'react-i18next';
import { Bot } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página do AI Assistant — placeholder para o assistente IA contextualizado.
 * A implementação completa integrará serviços, contratos, incidentes e runbooks
 * conforme definido em AI-ASSISTED-OPERATIONS.md.
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
