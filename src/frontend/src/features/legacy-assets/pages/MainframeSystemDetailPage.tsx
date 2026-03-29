import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  Database,
  Users,
  Globe,
  Shield,
  Layers,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { legacyAssetsApi } from '../api/legacyAssets';

/** Variantes visuais para badges de criticidade. */
const criticalityColors: Record<string, string> = {
  Critical: 'bg-red-900/40 text-red-300 border border-red-700/50',
  High: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Medium: 'bg-yellow-900/40 text-yellow-300 border border-yellow-700/50',
  Low: 'bg-slate-800/40 text-slate-300 border border-slate-700/50',
};

/** Variantes visuais para badges de ciclo de vida. */
const lifecycleColors: Record<string, string> = {
  Planning: 'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Development: 'bg-indigo-900/40 text-indigo-300 border border-indigo-700/50',
  Staging: 'bg-purple-900/40 text-purple-300 border border-purple-700/50',
  Active: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Deprecating: 'bg-amber-900/40 text-amber-300 border border-amber-700/50',
  Deprecated: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Retired: 'bg-slate-900/40 text-slate-400 border border-slate-700/50',
};

/** Página de detalhe de um ativo legacy (ex.: sistema mainframe). */
export function MainframeSystemDetailPage() {
  const { t } = useTranslation();
  const { assetType, assetId } = useParams<{ assetType: string; assetId: string }>();

  const {
    data: asset,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['legacy-asset-detail', assetType, assetId],
    queryFn: () => legacyAssetsApi.getDetail(assetType!, assetId!),
    enabled: !!assetType && !!assetId,
  });

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (isError || !asset) {
    return (
      <PageContainer>
        <PageErrorState />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      {/* ── Navegação e cabeçalho ── */}
      <div className="mb-6">
        <Link
          to="/services/legacy"
          className="inline-flex items-center gap-1 text-sm text-accent hover:underline mb-4"
        >
          <ArrowLeft size={14} />
          {t('legacyCatalog.detail.back')}
        </Link>
        <div className="flex items-center gap-3">
          <Database size={24} className="text-accent" />
          <div>
            <h1 className="text-2xl font-bold text-heading">{asset.displayName || asset.name}</h1>
            <p className="text-muted text-sm mt-0.5">{asset.assetType}</p>
          </div>
        </div>
      </div>

      {/* ── Informação geral ── */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Globe size={18} className="text-accent" />
              <h2 className="text-base font-semibold text-heading">{t('legacyCatalog.detail.generalInfo')}</h2>
            </div>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <InfoField label={t('legacyCatalog.card.domain')} value={asset.name} />
              {asset.description && (
                <InfoField label={t('legacyCatalog.description')} value={asset.description} />
              )}
              <InfoField label={t('legacyCatalog.card.domain')} value={asset.domain} />
              {asset.createdAt && (
                <InfoField label={t('legacyCatalog.card.created')} value={new Date(asset.createdAt).toLocaleDateString()} />
              )}
              {asset.updatedAt && (
                <InfoField label={t('legacyCatalog.card.updated')} value={new Date(asset.updatedAt).toLocaleDateString()} />
              )}
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* ── Ownership ── */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Users size={18} className="text-accent" />
              <h2 className="text-base font-semibold text-heading">{t('legacyCatalog.detail.ownership')}</h2>
            </div>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <InfoField label={t('legacyCatalog.card.team')} value={asset.teamName} />
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* ── Classificação ── */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Shield size={18} className="text-accent" />
              <h2 className="text-base font-semibold text-heading">{t('legacyCatalog.detail.classification')}</h2>
            </div>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <span className="text-xs text-muted block mb-1">{t('legacyCatalog.card.criticality')}</span>
                <span
                  className={`inline-flex text-xs px-2 py-0.5 rounded-full ${criticalityColors[asset.criticality] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}
                >
                  {t(`catalog.badges.criticality.${asset.criticality}`, { defaultValue: asset.criticality })}
                </span>
              </div>
              <div>
                <span className="text-xs text-muted block mb-1">{t('legacyCatalog.card.lifecycle')}</span>
                <span
                  className={`inline-flex text-xs px-2 py-0.5 rounded-full ${lifecycleColors[asset.lifecycleStatus] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}
                >
                  {t(`catalog.badges.lifecycle.${asset.lifecycleStatus}`, { defaultValue: asset.lifecycleStatus })}
                </span>
              </div>
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* ── Metadados técnicos ── */}
      {asset.metadata && Object.keys(asset.metadata).length > 0 && (
        <PageSection>
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Layers size={18} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('legacyCatalog.detail.technicalDetails')}</h2>
              </div>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {Object.entries(asset.metadata).map(([key, value]) => (
                  <InfoField key={key} label={key} value={value} />
                ))}
              </div>
            </CardBody>
          </Card>
        </PageSection>
      )}
    </PageContainer>
  );
}

/** Campo de informação reutilizável. */
function InfoField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-xs text-muted block mb-0.5">{label}</span>
      <span className="text-sm text-heading">{value}</span>
    </div>
  );
}

export default MainframeSystemDetailPage;
