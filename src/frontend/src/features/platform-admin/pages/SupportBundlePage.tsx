import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Download,
  PackageOpen,
  RefreshCw,
  AlertTriangle,
  CheckCircle2,
  Loader2,
  FileArchive,
  Clock,
  User,
} from 'lucide-react';
import { platformAdminApi } from '../api/platformAdmin';
import type { SupportBundleEntry } from '../api/platformAdmin';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function formatBytes(kb: number): string {
  if (kb < 1024) return `${kb} KB`;
  return `${(kb / 1024).toFixed(1)} MB`;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// ─── Bundle row ───────────────────────────────────────────────────────────────

function BundleRow({ bundle }: { bundle: SupportBundleEntry }) {
  const { t } = useTranslation();
  const downloadUrl = platformAdminApi.getSupportBundleDownloadUrl(bundle.id);

  return (
    <div className="flex items-center justify-between py-3 border-b border-border last:border-0">
      <div className="flex items-center gap-3 min-w-0">
        <FileArchive size={16} className="text-muted shrink-0" />
        <div className="min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-sm font-medium text-heading">
              {t('supportBundle.bundleTitle', { date: formatDate(bundle.generatedAt) })}
            </span>
            <span className="text-xs text-muted bg-surface border border-border rounded px-1.5 py-0.5">
              {formatBytes(bundle.fileSizeKb)}
            </span>
          </div>
          <div className="flex items-center gap-3 mt-0.5 text-xs text-muted">
            <span className="flex items-center gap-1">
              <User size={11} />
              {bundle.generatedBy}
            </span>
            <span className="flex items-center gap-1">
              <Clock size={11} />
              {formatDate(bundle.generatedAt)}
            </span>
          </div>
        </div>
      </div>
      <a
        href={downloadUrl}
        download
        className="flex items-center gap-1.5 text-sm text-accent hover:text-accent/80 transition-colors shrink-0 ml-4"
      >
        <Download size={14} />
        {t('supportBundle.download')}
      </a>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function SupportBundlePage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [generatedBundle, setGeneratedBundle] = useState<SupportBundleEntry | null>(null);

  const listQuery = useQuery({
    queryKey: ['support-bundles'],
    queryFn: platformAdminApi.getSupportBundles,
    staleTime: 60_000,
  });

  const generateMutation = useMutation({
    mutationFn: platformAdminApi.generateSupportBundle,
    onSuccess: (bundle) => {
      setGeneratedBundle(bundle);
      queryClient.invalidateQueries({ queryKey: ['support-bundles'] });
    },
  });

  const bundles = listQuery.data?.bundles ?? [];

  return (
    <div className="p-6 max-w-3xl mx-auto space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold text-heading">{t('supportBundle.title')}</h1>
        <p className="mt-1 text-sm text-muted">{t('supportBundle.subtitle')}</p>
      </div>

      {/* Info box — what's included */}
      <div className="bg-accent/5 border border-accent/20 rounded-xl p-4 space-y-3">
        <div className="flex items-center gap-2">
          <PackageOpen size={16} className="text-accent" />
          <span className="text-sm font-medium text-accent">{t('supportBundle.contentsTitle')}</span>
        </div>
        <ul className="grid grid-cols-1 sm:grid-cols-2 gap-1.5">
          {(t('supportBundle.contentsList', { returnObjects: true }) as string[]).map((item) => (
            <li key={item} className="flex items-center gap-2 text-xs text-muted">
              <CheckCircle2 size={12} className="text-success shrink-0" />
              {item}
            </li>
          ))}
        </ul>
        <p className="text-xs text-muted italic">{t('supportBundle.securityNote')}</p>
      </div>

      {/* Generate button */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3">
        <button
          onClick={() => {
            setGeneratedBundle(null);
            generateMutation.mutate();
          }}
          disabled={generateMutation.isPending}
          className="flex items-center gap-2 px-4 py-2 bg-accent text-white rounded-lg text-sm font-medium hover:bg-accent/90 transition-colors disabled:opacity-60"
        >
          {generateMutation.isPending ? (
            <Loader2 size={14} className="animate-spin" />
          ) : (
            <PackageOpen size={14} />
          )}
          {generateMutation.isPending
            ? t('supportBundle.generating')
            : t('supportBundle.generate')}
        </button>
        {generateMutation.isError && (
          <div className="flex items-center gap-2 text-sm text-critical">
            <AlertTriangle size={14} />
            {t('supportBundle.generateError')}
          </div>
        )}
      </div>

      {/* Just-generated bundle download */}
      {generatedBundle && !generateMutation.isPending && (
        <div className="flex items-center justify-between bg-success/10 border border-success/20 rounded-xl p-4">
          <div className="flex items-center gap-3">
            <CheckCircle2 size={18} className="text-success" />
            <div>
              <p className="text-sm font-medium text-heading">{t('supportBundle.readyTitle')}</p>
              <p className="text-xs text-muted">{formatBytes(generatedBundle.fileSizeKb)}</p>
            </div>
          </div>
          <a
            href={platformAdminApi.getSupportBundleDownloadUrl(generatedBundle.id)}
            download
            className="flex items-center gap-1.5 text-sm font-medium text-accent hover:text-accent/80 transition-colors"
          >
            <Download size={14} />
            {t('supportBundle.downloadNow')}
          </a>
        </div>
      )}

      {/* History */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-base font-semibold text-heading">{t('supportBundle.historyTitle')}</h2>
          <button
            onClick={() => listQuery.refetch()}
            className="flex items-center gap-1 text-xs text-muted hover:text-heading transition-colors"
            aria-label={t('supportBundle.refresh')}
          >
            <RefreshCw size={12} className={listQuery.isFetching ? 'animate-spin' : ''} />
            {t('supportBundle.refresh')}
          </button>
        </div>

        <div className="bg-card border border-border rounded-xl divide-y divide-border">
          {listQuery.isLoading ? (
            <div className="flex items-center justify-center py-8 gap-2 text-muted">
              <Loader2 size={16} className="animate-spin" />
              <span className="text-sm">{t('supportBundle.loadingHistory')}</span>
            </div>
          ) : listQuery.isError ? (
            <div className="flex items-center justify-center gap-2 py-8 text-critical text-sm">
              <AlertTriangle size={14} />
              {t('supportBundle.loadError')}
            </div>
          ) : bundles.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-10 text-muted text-sm gap-2">
              <FileArchive size={24} className="text-muted/40" />
              {t('supportBundle.emptyHistory')}
            </div>
          ) : (
            <div className="px-4">
              {bundles.map((b) => (
                <BundleRow key={b.id} bundle={b} />
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
