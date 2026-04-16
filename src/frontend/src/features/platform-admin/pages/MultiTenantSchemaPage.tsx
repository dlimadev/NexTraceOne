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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Server size={24} className="text-teal-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {/* Info banner */}
      <div className="flex items-start gap-3 p-4 bg-teal-50 border border-teal-200 rounded-lg text-sm text-teal-800">
        <AlertTriangle size={16} className="mt-0.5 shrink-0" />
        <p>{t('schemaBanner')}</p>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-2 gap-4">
        <div className="bg-white border border-slate-200 rounded-lg p-4">
          <p className="text-xs text-slate-500">{t('totalSchemas')}</p>
          <p className="text-2xl font-semibold text-slate-900 mt-1">{schemas.length}</p>
        </div>
        <div className="bg-white border border-slate-200 rounded-lg p-4">
          <p className="text-xs text-slate-500">{t('isolationMode')}</p>
          <p className="text-sm font-semibold text-teal-700 mt-1">{t('schemaPerTenant')}</p>
        </div>
      </div>

      {/* Provision new schema */}
      <div className="bg-white border border-slate-200 rounded-lg p-5">
        <h2 className="text-sm font-semibold text-slate-900 mb-3">{t('provisionTitle')}</h2>
        <div className="flex items-start gap-3">
          <div className="flex-1">
            <input
              type="text"
              value={newSlug}
              onChange={(e) => setNewSlug(e.target.value.toLowerCase())}
              placeholder={t('slugPlaceholder')}
              className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500"
            />
            {provisionError && (
              <p className="mt-1 text-xs text-red-600">{provisionError}</p>
            )}
            <p className="mt-1 text-xs text-slate-400">{t('slugHint')}</p>
          </div>
          <button
            onClick={handleProvision}
            disabled={provisionMutation.isPending}
            className="flex items-center gap-2 px-4 py-2 text-sm bg-teal-600 text-white rounded-lg hover:bg-teal-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
          >
            <Plus size={14} />
            {provisionMutation.isPending ? t('provisioning') : t('provision')}
          </button>
        </div>
        {provisionMutation.isSuccess && (
          <div className="mt-3 flex items-center gap-2 text-green-700 text-sm">
            <CheckCircle size={14} />
            {t('provisionSuccess')}
          </div>
        )}
      </div>

      {isLoading && (
        <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
          {t('loading')}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <XCircle size={18} />
          {t('error')}
        </div>
      )}

      {data && schemas.length === 0 && (
        <div className="flex flex-col items-center justify-center h-32 text-slate-400 text-sm gap-2">
          <Database size={28} className="text-slate-300" />
          <p>{t('noSchemas')}</p>
        </div>
      )}

      {data && schemas.length > 0 && (
        <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200">
                <th className="text-left px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">
                  {t('colTenantSlug')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">
                  {t('colSchemaName')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">
                  {t('colSearchPath')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {schemas.map((schema) => (
                <SchemaRow key={schema.tenantSlug} schema={schema} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function SchemaRow({ schema }: { schema: TenantSchemaEntry }) {
  return (
    <tr className="hover:bg-slate-50">
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <CheckCircle size={14} className="text-green-500 shrink-0" />
          <span className="font-medium text-slate-900">{schema.tenantSlug}</span>
        </div>
      </td>
      <td className="px-4 py-3 font-mono text-xs text-slate-600">{schema.schemaName}</td>
      <td className="px-4 py-3 font-mono text-xs text-slate-400">{schema.searchPath}</td>
    </tr>
  );
}
