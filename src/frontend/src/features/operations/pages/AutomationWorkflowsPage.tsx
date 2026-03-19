import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { ShieldAlert, Zap } from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer } from '../../../components/shell';

/**
 * Página de Automation Workflows.
 * Nesta fase a experiência permanece em preview e não expõe catálogos ou execuções mockadas
 * como se fossem workflows operacionais reais.
 */
export function AutomationWorkflowsPage() {
  const { t } = useTranslation();

  return (
    <PageContainer>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('automation.title')}</h1>
        <p className="text-muted mt-1">{t('automation.subtitle')}</p>
        <div className="flex items-center gap-2 mt-2">
          <Badge variant="warning">{t('governance.preview.badge')}</Badge>
          <span className="text-xs text-muted">
            {t(
              'automation.preview.reason',
              'Automation workflows remain in preview and are not backed by production workflow data in this release.',
            )}
          </span>
        </div>
      </div>

      <div className="mb-6 rounded-md border border-warning/30 bg-warning/5 p-3 text-xs text-muted flex items-center gap-2">
        <ShieldAlert size={14} className="text-warning shrink-0" />
        <span>
          {t('automation.adminHint')}{' '}
          <NavLink to="/operations/automation/admin" className="text-accent hover:text-accent/80 transition-colors">
            {t('automation.adminLink')}
          </NavLink>
        </span>
      </div>

      <EmptyState
        icon={<Zap size={24} />}
        title={t('automation.preview.title', 'Automation workflow execution is not available yet')}
        description={t(
          'automation.preview.description',
          'The operational automation catalog and workflow history were removed from the productive scope until they are backed by real workflow orchestration data.',
        )}
        action={
          <NavLink
            to="/operations/incidents"
            className="inline-flex items-center gap-2 rounded-md border border-accent/30 bg-accent/10 px-4 py-2 text-sm font-medium text-accent hover:bg-accent/15 transition-colors"
          >
            <ShieldAlert size={14} />
            {t('automation.preview.fallbackAction', 'Open incidents instead')}
          </NavLink>
        }
      />
    </PageContainer>
  );
}
