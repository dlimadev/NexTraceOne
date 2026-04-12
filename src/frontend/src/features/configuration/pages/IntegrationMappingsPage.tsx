import { useTranslation } from 'react-i18next';
import { ArrowRight, Map } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';

/**
 * IntegrationMappingsPage — placeholder para configuração de mapeamentos de campos por conector.
 * A funcionalidade completa é gerida por conector no módulo de Integrações.
 * Pilar: Platform Customization — Integrations & API (roadmap)
 */
export function IntegrationMappingsPage() {
  const { t } = useTranslation();

  return (
    <PageContainer>
      <PageHeader
        title={t('integrationMappings.title')}
        badge={<Badge variant="info">{t('preview.label', 'Preview')}</Badge>}
      />

      <Card>
        <CardBody className="flex flex-col items-start gap-4 py-8">
          <div className="flex items-center gap-3 text-muted-foreground">
            <Map size={32} className="text-blue-500 opacity-70" />
            <p className="text-sm">{t('integrationMappings.description')}</p>
          </div>
          <a
            href="/platform/integrations"
            className="inline-flex items-center gap-1.5 text-sm text-blue-600 dark:text-blue-400 hover:underline font-medium"
          >
            {t('integrationMappings.title')}
            <ArrowRight size={14} />
          </a>
        </CardBody>
      </Card>
    </PageContainer>
  );
}

export default IntegrationMappingsPage;
