import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface AnnotationDto {
  id: string;
  timestamp: string;
  type: string;
  title: string;
  detail?: string;
  serviceName?: string;
  severity: 'critical' | 'warning' | 'info';
  isSimulated: boolean;
}

interface AnnotationSourceSummary {
  source: string;
  count: number;
  isSimulated: boolean;
  simulatedNote?: string;
}

interface AnnotationsResponse {
  annotations: AnnotationDto[];
  sources: AnnotationSourceSummary[];
  from: string;
  to: string;
  totalCount: number;
}

interface AnnotationsOverlayProps {
  tenantId: string;
  from: string;
  to: string;
  serviceNames?: string[];
  enabled?: boolean;
}

// ── Severity styling ──────────────────────────────────────────────────────

function severityClasses(severity: AnnotationDto['severity']) {
  return {
    critical: 'border-red-600 bg-red-950 text-red-300',
    warning:  'border-yellow-600 bg-yellow-950 text-yellow-300',
    info:     'border-blue-600 bg-blue-950 text-blue-300',
  }[severity];
}

function severityDot(severity: AnnotationDto['severity']) {
  return {
    critical: 'bg-red-500',
    warning:  'bg-yellow-500',
    info:     'bg-blue-500',
  }[severity];
}

function typeIcon(type: string): string {
  if (type.startsWith('change'))   return '🚀';
  if (type.startsWith('incident')) return '🔥';
  if (type.startsWith('contract')) return '📋';
  if (type.startsWith('policy'))   return '⚠';
  return '•';
}

// ── Component ─────────────────────────────────────────────────────────────

export function AnnotationsOverlay({
  tenantId,
  from,
  to,
  serviceNames,
  enabled = true,
}: AnnotationsOverlayProps) {
  const { t } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const [selectedType, setSelectedType] = useState<string | null>(null);

  const { data, isLoading } = useQuery<AnnotationsResponse>({
    queryKey: ['dashboard-annotations', tenantId, from, to, serviceNames?.join(',')],
    queryFn: async () => {
      const params = new URLSearchParams({
        tenantId,
        from,
        to,
        maxPerSource: '50',
      });
      if (serviceNames?.length) params.set('services', serviceNames.join(','));
      const res = await client.get(`/api/v1/governance/dashboards/annotations?${params}`);
      return res.data;
    },
    enabled,
    staleTime: 60_000,
  });

  const annotations = data?.annotations ?? [];
  const sources = data?.sources ?? [];
  const anySimulated = sources.some(s => s.isSimulated && s.count > 0);

  const filtered = selectedType
    ? annotations.filter(a => a.type.startsWith(selectedType))
    : annotations;

  const typeOptions = [...new Set(annotations.map(a => a.type.split('.')[0]))];

  return (
    <div className="relative">
      {/* Toggle button */}
      <button
        type="button"
        onClick={() => setIsOpen(prev => !prev)}
        className={`flex items-center gap-1.5 rounded border px-2 py-1 text-xs transition-colors ${
          isOpen
            ? 'border-blue-600 bg-blue-950 text-blue-300'
            : 'border-neutral-700 bg-neutral-800 text-neutral-400 hover:text-neutral-200'
        }`}
        aria-expanded={isOpen}
        aria-label={t('dashboardAnnotations.toggleLabel')}
      >
        <span>📌</span>
        <span>{t('dashboardAnnotations.title')}</span>
        {data && data.totalCount > 0 && (
          <span className="rounded-full bg-blue-600 px-1.5 py-0.5 text-xs text-white leading-none">
            {data.totalCount}
          </span>
        )}
      </button>

      {/* Dropdown panel */}
      {isOpen && (
        <div className="absolute right-0 top-full z-50 mt-1 w-96 rounded-lg border border-neutral-700 bg-neutral-900 shadow-xl">
          <div className="flex items-center justify-between border-b border-neutral-700 px-3 py-2">
            <h3 className="text-sm font-semibold text-neutral-100">
              {t('dashboardAnnotations.title')}
            </h3>
            <button
              type="button"
              onClick={() => setIsOpen(false)}
              className="text-neutral-500 hover:text-neutral-300 text-sm"
              aria-label="Close"
            >
              ✕
            </button>
          </div>

          {/* Simulated banner */}
          {anySimulated && (
            <div className="border-b border-yellow-800 bg-yellow-950 px-3 py-2 text-xs text-yellow-300">
              ⚠ {t('dashboardAnnotations.simulatedBanner')} — {t('dashboardAnnotations.simulatedNote')}
            </div>
          )}

          {/* Type filter */}
          {typeOptions.length > 1 && (
            <div className="flex gap-1 border-b border-neutral-800 px-3 py-2 flex-wrap">
              <button
                type="button"
                onClick={() => setSelectedType(null)}
                className={`rounded px-2 py-0.5 text-xs ${!selectedType ? 'bg-blue-700 text-white' : 'bg-neutral-800 text-neutral-400 hover:bg-neutral-700'}`}
              >
                All
              </button>
              {typeOptions.map(type => (
                <button
                  key={type}
                  type="button"
                  onClick={() => setSelectedType(prev => prev === type ? null : type)}
                  className={`rounded px-2 py-0.5 text-xs capitalize ${
                    selectedType === type
                      ? 'bg-blue-700 text-white'
                      : 'bg-neutral-800 text-neutral-400 hover:bg-neutral-700'
                  }`}
                >
                  {type}
                </button>
              ))}
            </div>
          )}

          {/* Annotation list */}
          <div className="max-h-80 overflow-y-auto">
            {isLoading ? (
              <p className="px-3 py-4 text-center text-sm text-neutral-500">
                {t('dashboardAnnotations.loadingAnnotations')}
              </p>
            ) : filtered.length === 0 ? (
              <p className="px-3 py-4 text-center text-sm text-neutral-500">
                {t('dashboardAnnotations.noAnnotations')}
              </p>
            ) : (
              <ul className="divide-y divide-neutral-800">
                {filtered.map(ann => (
                  <li key={ann.id} className="px-3 py-2 hover:bg-neutral-800/40">
                    <div className="flex items-start gap-2">
                      <span className="mt-0.5 shrink-0 text-base leading-none">{typeIcon(ann.type)}</span>
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-1.5">
                          <span className={`inline-block h-1.5 w-1.5 shrink-0 rounded-full ${severityDot(ann.severity)}`} />
                          <p className="text-xs font-medium text-neutral-200 truncate">{ann.title}</p>
                        </div>
                        {ann.detail && (
                          <p className="mt-0.5 text-xs text-neutral-400 line-clamp-2">{ann.detail}</p>
                        )}
                        <div className="mt-1 flex items-center gap-2 text-xs text-neutral-500">
                          <time>{new Date(ann.timestamp).toLocaleString()}</time>
                          {ann.serviceName && (
                            <span className="rounded bg-neutral-800 px-1 font-mono">{ann.serviceName}</span>
                          )}
                        </div>
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {/* Sources summary */}
          {sources.length > 0 && (
            <div className="border-t border-neutral-800 px-3 py-2">
              <p className="text-xs text-neutral-500">
                {sources.filter(s => s.count > 0).map(s => `${s.source}: ${s.count}`).join(' · ')}
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
