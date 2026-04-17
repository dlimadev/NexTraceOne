import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { History, Download, Filter, Clock } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { EmptyState } from '../../../components/EmptyState';
import { configurationApi } from '../../configuration/api/configurationApi';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/**
 * ReleaseParameterAuditPage — trilha de auditoria dos parâmetros de controlo de release.
 *
 * Permite auditores, platform admins e tech leads:
 * - Visualizar todas as alterações em parâmetros de release (quem, quando, valor anterior / novo)
 * - Filtrar por chave de parâmetro e escopo (System, Tenant, Environment)
 * - Exportar o histórico em CSV para relatórios de compliance
 *
 * Consome o endpoint /configuration/audit-history com filtro de prefixo "change.release."
 * para restringir ao escopo do módulo de release.
 *
 * Personas beneficiadas: Auditor, Platform Admin, Tech Lead, Architect.
 */
export function ReleaseParameterAuditPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [filterKey, setFilterKey] = useState('');
  const [scopeFilter, setScopeFilter] = useState('all');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['release-parameter-audit', scopeFilter, activeEnvironmentId],
    queryFn: () => configurationApi.getReleaseParameterAudit('change.release.', 200),
  });

  const scopes = ['all', 'System', 'Tenant', 'Environment'];

  const entries = (data ?? []).filter(entry => {
    const matchesKey = filterKey === '' || entry.key.toLowerCase().includes(filterKey.toLowerCase());
    const matchesScope = scopeFilter === 'all' || entry.scope === scopeFilter;
    return matchesKey && matchesScope;
  });

  const handleExport = () => {
    if (!entries.length) return;
    const header = [
      t('releaseParameterAudit.colParameter'),
      t('releaseParameterAudit.colPreviousValue'),
      t('releaseParameterAudit.colNewValue'),
      t('releaseParameterAudit.colChangedBy'),
      t('releaseParameterAudit.colChangedAt'),
      t('releaseParameterAudit.colScope'),
      t('releaseParameterAudit.colReason'),
    ].join(',');
    const rows = entries.map(e =>
      [
        e.key,
        e.previousValue ?? '',
        e.newValue ?? '',
        e.changedBy ?? '',
        e.changedAt ?? '',
        e.scope ?? '',
        e.changeReason ?? '',
      ]
        .map(v => `"${String(v).replace(/"/g, '""')}"`)
        .join(','),
    );
    const csv = [header, ...rows].join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'release-parameter-audit.csv';
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <PageContainer>
      <PageHeader
        icon={<History className="w-6 h-6 text-accent" />}
        title={t('releaseParameterAudit.title')}
        subtitle={t('releaseParameterAudit.subtitle')}
        actions={
          <button
            onClick={handleExport}
            disabled={!entries.length}
            className="inline-flex items-center gap-2 rounded-md border border-edge bg-surface px-4 py-2 text-sm font-medium text-heading hover:bg-surface/80 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <Download className="w-4 h-4" />
            {t('releaseParameterAudit.exportButton')}
          </button>
        }
      />

      <Card>
        <CardHeader>
          <div className="flex flex-col sm:flex-row gap-3 sm:items-center">
            <div className="flex items-center gap-2 flex-1">
              <Filter className="w-4 h-4 text-muted" />
              <input
                type="text"
                value={filterKey}
                onChange={e => setFilterKey(e.target.value)}
                placeholder={t('releaseParameterAudit.filterLabel')}
                className="flex-1 rounded-md bg-canvas border border-edge px-3 py-1.5 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
              />
            </div>
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted">{t('releaseParameterAudit.scopeFilter')}:</span>
              <div className="flex gap-1">
                {scopes.map(s => (
                  <button
                    key={s}
                    onClick={() => setScopeFilter(s)}
                    className={`rounded px-2.5 py-1 text-xs font-medium transition-colors ${
                      scopeFilter === s
                        ? 'bg-accent text-white'
                        : 'bg-surface border border-edge text-muted hover:text-heading'
                    }`}
                  >
                    {s === 'all' ? t('releaseParameterAudit.allScopes') : s}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </CardHeader>
        <CardBody>
          {isLoading && <PageLoadingState message={t('releaseParameterAudit.loading')} />}
          {isError && (
            <EmptyState
              icon={<History className="w-10 h-10" />}
              title={t('releaseParameterAudit.errorTitle')}
              description={t('releaseParameterAudit.errorDescription')}
            />
          )}
          {!isLoading && !isError && entries.length === 0 && (
            <EmptyState
              icon={<Clock className="w-10 h-10" />}
              title={t('releaseParameterAudit.emptyTitle')}
              description={t('releaseParameterAudit.emptyDescription')}
            />
          )}
          {!isLoading && !isError && entries.length > 0 && (
            <div className="overflow-x-auto -mx-4 sm:mx-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-xs text-muted uppercase tracking-wide">
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterAudit.colParameter')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterAudit.colPreviousValue')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterAudit.colNewValue')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterAudit.colChangedBy')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterAudit.colChangedAt')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterAudit.colScope')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterAudit.colReason')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge">
                  {entries.map((entry, idx) => (
                    <tr key={`${entry.key}-${idx}`} className="hover:bg-surface/50 transition-colors">
                      <td className="px-4 py-3 font-mono text-xs text-heading">{entry.key}</td>
                      <td className="px-4 py-3 text-muted font-mono text-xs max-w-[120px] truncate">
                        {entry.previousValue ?? '—'}
                      </td>
                      <td className="px-4 py-3 font-mono text-xs text-heading max-w-[120px] truncate">
                        {entry.newValue ?? '—'}
                      </td>
                      <td className="px-4 py-3 text-muted text-xs">{entry.changedBy ?? '—'}</td>
                      <td className="px-4 py-3 text-muted text-xs whitespace-nowrap">
                        {entry.changedAt
                          ? new Date(entry.changedAt).toLocaleString()
                          : '—'}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant="neutral" size="sm">{entry.scope ?? '—'}</Badge>
                      </td>
                      <td className="px-4 py-3 text-muted text-xs max-w-[160px] truncate">
                        {entry.changeReason ?? '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardBody>
      </Card>
    </PageContainer>
  );
}
