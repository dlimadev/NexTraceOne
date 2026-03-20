import { useTranslation } from 'react-i18next';
import { BookOpen } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

/**
 * Página de Runbooks — procedimentos operacionais e guias de mitigação.
 * Parte do módulo Operations do NexTraceOne.
 */
export function RunbooksPage() {
  const { t } = useTranslation();

  return (
    <PageContainer>
      <PageHeader
        title={t('runbooks.title')}
        subtitle={t('runbooks.subtitle')}
      />

      <PageSection>
        <OnboardingHints module="operations" />
      </PageSection>

      <PageSection>
      <Card>
        <CardBody>
          <EmptyState
            icon={<BookOpen size={24} />}
            title={t('runbooks.emptyTitle')}
            description={t('productPolish.emptyRunbooks')}
            action={
              <Link
                to="/operations/incidents"
                className="text-sm text-accent hover:text-accent/80 transition-colors"
              >
                {t('common.viewAll')} → {t('sidebar.incidents')}
              </Link>
            }
          />
        </CardBody>
      </Card>
      </PageSection>
    </PageContainer>
  );
}
