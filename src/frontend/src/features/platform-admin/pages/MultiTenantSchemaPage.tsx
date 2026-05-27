import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Database,
  RefreshCw,
  XCircle,
  CheckCircle,
  Plus,
  Server,
  AlertTriangle,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type TenantSchemaEntry } from '../api/platformAdmin';

export function MultiTenantSchemaPage() {
  const { t } = useTranslation('multiTenantSchema');
  const queryClient = useQueryClient();
  const [newSlug, setNewSlug] = useState('');
  const [provisionError, setProvisionError] = useState('');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['tenant-schemas'],
    queryFn: platformAdminApi.getTenantSchemas,
  });

  const provisionMutation = useMutation({
    mutationFn: (tenantSlug: string) =>
      platformAdminApi.provisionTenantSchema({ tenantSlug }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant-schemas'] });
      setNewSlug('');
      setProvisionError('');
    },
    onError: () => {
      setProvisionError(t('provisionError'));
    },
  });

  const handleProvision = () => {
    const slug = newSlug.trim();
    if (!slug) {
      setProvisionError(t('slugRequired'));
      return;
    }
    if (!/^[a-z0-9_-]+$/.test(slug)) {
      setProvisionError(t('slugInvalid'));
      return;
    }
    setProvisionError('');
    provisionMutation.mutate(slug);
  };

  const schemas = data?.schemas ?? [];

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Server size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {/* Info banner */}
        <div className="flex items-start gap-3 p-4 bg-accent/10 border border-accent/20 rounded-lg text-sm text-accent">
          <AlertTriangle size={16} className="mt-0.5 shrink-0" />
          <p>{t('schemaBanner')}</p>
        </div>

        {/* Summary */}
        <div className="grid grid-cols-2 gap-4">
          <div className="bg-card border border-edge rounded-lg p-4">
            <p className="text-xs text-muted">{t('totalSchemas')}</p>
            <p className="text-2xl font-semibold text-heading mt-1">{schemas.length}</p>
          </div>
          <div className="bg-card border border-edge rounded-lg p-4">
            <p className="text-xs text-muted">{t('isolationMode')}</p>
            <p className="text-sm font-semibold text-accent mt-1">{t('schemaPerTenant')}</p>
          </div>
        </div>

        {/* Provision new schema */}
        <div className="bg-card border border-edge rounded-lg p-5">
          <h2 className="text-sm font-semibold text-heading mb-3">{t('provisionTitle')}</h2>
          <div className="flex items-start gap-3">
            <div className="flex-1">
              <input
                type="text"
                value={newSlug}
                onChange={(e) => setNewSlug(e.target.value.toLowerCase())}
                placeholder={t('slugPlaceholder')}
                className="w-full px-3 py-2 text-sm border border-edge rounded-lg bg-canvas text-body focus:outline-none focus:ring-2 focus:ring-accent/50"
              />
              {provisionError && (
                <p className="mt-1 text-xs text-critical">{provisionError}</p>
              )}
              <p className="mt-1 text-xs text-faded">{t('slugHint')}</p>
            </div>
            <button
              onClick={handleProvision}
              disabled={provisionMutation.isPending}
              className="flex items-center gap-2 px-4 py-2 text-sm bg-accent text-white rounded-lg hover:bg-accent/90 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
            >
              <Plus size={14} />
              {provisionMutation.isPending ? t('provisioning') : t('provision')}
            </button>
          </div>
          {provisionMutation.isSuccess && (
            <div className="mt-3 flex items-center gap-2 text-success text-sm">
              <CheckCircle size={14} />
              {t('provisionSuccess')}
            </div>
          )}
        </div>

        {isLoading && (
          <div className="flex items-center justify-center h-48 text-faded text-sm">
            {t('loading')}
          </div>
        )}

        {isError && (
          <div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical text-sm">
            <XCircle size={18} />
            {t('error')}
          </div>
        )}

        {data && schemas.length === 0 && (
          <div className="flex flex-col items-center justify-center h-32 text-faded text-sm gap-2">
            <Database size={28} className="text-faded" />
            <p>{t('noSchemas')}</p>
          </div>
        )}

        {data && schemas.length > 0 && (
          <div className="bg-card border border-edge rounded-lg overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-elevated border-b border-edge">
                  <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wide">
                    {t('colTenantSlug')}
                  </th>
                  <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wide">
                    {t('colSchemaName')}
                  </th>
                  <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wide">
                    {t('colSearchPath')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge/50">
                {schemas.map((schema) => (
                  <SchemaRow key={schema.tenantSlug} schema={schema} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </PageContainer>
  );
}

function SchemaRow({ schema }: { schema: TenantSchemaEntry }) {
  return (
    <tr className="hover:bg-elevated">
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <CheckCircle size={14} className="text-success shrink-0" />
          <span className="font-medium text-heading">{schema.tenantSlug}</span>
        </div>
      </td>
      <td className="px-4 py-3 font-mono text-xs text-muted">{schema.schemaName}</td>
      <td className="px-4 py-3 font-mono text-xs text-faded">{schema.searchPath}</td>
    </tr>
  );
}
