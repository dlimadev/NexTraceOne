import { useTranslation } from 'react-i18next';
import { BookOpen } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Página de Runbooks — procedimentos operacionais e guias de mitigação.
 * Parte do módulo Operations do NexTraceOne.
 */
export function RunbooksPage() {
  const { t } = useTranslation();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('runbooks.title')}</h1>
        <p className="text-muted mt-1">{t('runbooks.subtitle')}</p>
      </div>
      <Card>
        <CardBody>
          <EmptyState
            icon={<BookOpen size={24} />}
            title={t('runbooks.emptyTitle')}
            description={t('runbooks.emptyDescription')}
          />
        </CardBody>
      </Card>
    </div>
  );
}
