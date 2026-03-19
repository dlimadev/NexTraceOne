import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, NavLink } from 'react-router-dom';
import { ArrowLeft, Zap } from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer } from '../../../components/shell';

/**
 * Página de detalhe de Automation Workflow.
 * Nesta fase a capability permanece em preview e não expõe narrativas de execução
 * simuladas como se fossem workflows reais.
 */
export function AutomationWorkflowDetailPage() {
  const { t } = useTranslation();
  const { workflowId } = useParams<{ workflowId: string }>();

  return (
    <PageContainer>
      <NavLink to="/operations/automation" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
        <ArrowLeft size={16} />
        {t('automation.detail.backToList')}
      </NavLink>

      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading flex items-center gap-2">
          <Zap size={22} className="text-accent" />
          {t('automation.detail.subtitle', { id: workflowId ?? 'preview' })}
        </h1>
        <div className="flex items-center gap-2 mt-2">
          <Badge variant="warning">{t('governance.preview.badge')}</Badge>
          <span className="text-xs text-muted">
            {t(
              'automation.preview.detailReason',
              'Workflow detail remains a preview stub until the execution model is backed by real automation state and audit data.',
            )}
          </span>
        </div>
      </div>

      <EmptyState
        icon={<Zap size={24} />}
        title={t('automation.preview.detailTitle', 'Workflow detail is not available in the productive scope')}
        description={t(
          'automation.preview.detailDescription',
          'Mock execution steps, approvals and audit entries were removed from this page to avoid exposing simulated workflow state as if it were operational truth.',
        )}
        action={
          <NavLink
            to="/operations/automation"
            className="inline-flex items-center gap-2 rounded-md border border-accent/30 bg-accent/10 px-4 py-2 text-sm font-medium text-accent hover:bg-accent/15 transition-colors"
          >
            <ArrowLeft size={14} />
            {t('automation.detail.backToList')}
          </NavLink>
        }
      />
    </PageContainer>
  );
}
