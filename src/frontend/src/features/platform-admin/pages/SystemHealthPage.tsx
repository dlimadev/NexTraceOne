import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Activity,
  CheckCircle,
  RefreshCw,
  XCircle,
  ExternalLink,
  AlertCircle,
} from 'lucide-react';
import {
  platformAdminApi,
  type OptionalProviderDto,
  type OptionalProviderStatus,
} from '../api/platformAdmin';

/**
 * Base URL used to resolve the "Setup docs" link for each optional provider.
 *
 * Defaults to the public upstream repository, but can be overridden at build time
 * via `VITE_DOCS_BASE_URL` to support forks, internal mirrors, or offline bundles.
 * Examples:
 *   - `https://github.com/your-org/NexTraceOne/blob/main`
 *   - `https://internal-docs.company.com/nextraceone`
 *   - `/docs` (bundled static docs served by the ApiHost)
 */
const DOCS_BASE_URL: string =
  import.meta.env.VITE_DOCS_BASE_URL ?? 'https://github.com/dlimadev/NexTraceOne/blob/main';

/**
 * SystemHealthPage — CFG-01
 *
 * Lista cada provider opcional da plataforma e o seu estado (configured /
 * not-configured). Permite que Platform Admins compreendam imediatamente o
 * porquê de verem `simulatedNote` noutras páginas (canary, backup, kafka, …).
 *
 * Fonte da verdade: backend `GetOptionalProviders` em Governance module.
 */
export function SystemHealthPage() {
  const { t } = useTranslation('systemHealth');

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['system-health-optional-providers'],
    queryFn: platformAdminApi.getOptionalProviders,
  });

  const providers = data?.providers ?? [];
  const configured = data?.configuredCount ?? 0;
  const total = data?.totalCount ?? 0;

  const groupedByCategory = providers.reduce<Record<string, OptionalProviderDto[]>>(
    (acc, provider) => {
      (acc[provider.category] ??= []).push(provider);
      return acc;
    },
    {},
  );

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Activity size={24} className="text-indigo-600" aria-hidden="true" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          type="button"
          onClick={() => refetch()}
          disabled={isFetching}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50"
        >
          <RefreshCw
            size={14}
            className={isFetching ? 'animate-spin' : undefined}
            aria-hidden="true"
          />
          {t('actions.refresh')}
        </button>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <SummaryCard
          label={t('summary.configured')}
          value={configured}
          tone="positive"
          icon={<CheckCircle size={18} aria-hidden="true" />}
        />
        <SummaryCard
          label={t('summary.notConfigured')}
          value={total - configured}
          tone="neutral"
          icon={<XCircle size={18} aria-hidden="true" />}
        />
        <SummaryCard
          label={t('summary.total')}
          value={total}
          tone="informative"
          icon={<Activity size={18} aria-hidden="true" />}
        />
      </div>

      {/* States */}
      {isLoading && (
        <div className="rounded-lg border border-slate-200 p-6 text-sm text-slate-600">
          {t('states.loading')}
        </div>
      )}
      {isError && (
        <div
          role="alert"
          className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700 flex items-start gap-2"
        >
          <AlertCircle size={18} aria-hidden="true" />
          <span>{t('states.error')}</span>
        </div>
      )}
      {!isLoading && !isError && providers.length === 0 && (
        <div className="rounded-lg border border-slate-200 p-6 text-sm text-slate-500">
          {t('states.empty')}
        </div>
      )}

      {/* Providers grouped by category */}
      {!isLoading &&
        !isError &&
        Object.entries(groupedByCategory).map(([category, items]) => (
          <section key={category} className="space-y-3">
            <h2 className="text-sm font-medium uppercase tracking-wide text-slate-500">
              {t(`categories.${category}`, { defaultValue: category })}
            </h2>
            <ul className="space-y-2">
              {items.map((provider) => (
                <li
                  key={provider.name}
                  className="rounded-lg border border-slate-200 bg-white p-4 flex items-start gap-3"
                >
                  <StatusBadge status={provider.status} label={t(`status.${provider.status}`)} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-slate-900">
                        {t(`providers.${provider.name}.name`, { defaultValue: provider.name })}
                      </span>
                      <code className="text-xs text-slate-500">{provider.configKeyPrefix}</code>
                    </div>
                    <p className="mt-1 text-sm text-slate-600">{provider.description}</p>
                  </div>
                  <a
                    href={`${DOCS_BASE_URL}/${provider.docsPath}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-1 text-xs text-indigo-600 hover:underline"
                  >
                    {t('actions.setupDocs')}
                    <ExternalLink size={12} aria-hidden="true" />
                  </a>
                </li>
              ))}
            </ul>
          </section>
        ))}
    </div>
  );
}

interface SummaryCardProps {
  label: string;
  value: number;
  tone: 'positive' | 'neutral' | 'informative';
  icon: React.ReactNode;
}

function SummaryCard({ label, value, tone, icon }: SummaryCardProps) {
  const toneClass =
    tone === 'positive'
      ? 'text-emerald-700 bg-emerald-50 border-emerald-200'
      : tone === 'neutral'
        ? 'text-slate-700 bg-slate-50 border-slate-200'
        : 'text-indigo-700 bg-indigo-50 border-indigo-200';

  return (
    <div className={`rounded-lg border p-4 ${toneClass}`}>
      <div className="flex items-center gap-2 text-sm">{icon}<span>{label}</span></div>
      <div className="mt-2 text-2xl font-semibold">{value}</div>
    </div>
  );
}

interface StatusBadgeProps {
  status: OptionalProviderStatus;
  label: string;
}

function StatusBadge({ status, label }: StatusBadgeProps) {
  if (status === 'Configured') {
    return (
      <span className="mt-0.5 inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-700 border border-emerald-200">
        <CheckCircle size={12} aria-hidden="true" />
        {label}
      </span>
    );
  }
  if (status === 'Unknown') {
    return (
      <span className="mt-0.5 inline-flex items-center gap-1 rounded-full bg-amber-50 px-2.5 py-1 text-xs font-medium text-amber-700 border border-amber-200">
        <AlertCircle size={12} aria-hidden="true" />
        {label}
      </span>
    );
  }
  return (
    <span className="mt-0.5 inline-flex items-center gap-1 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-700 border border-slate-200">
      <XCircle size={12} aria-hidden="true" />
      {label}
    </span>
  );
}
