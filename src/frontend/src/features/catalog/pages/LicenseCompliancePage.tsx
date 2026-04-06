import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { FileCheck } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

type ActiveAction = 'licenses' | 'upgrades' | 'sbom' | null;

interface LicenseConflict {
  packageName: string;
  licenseId: string;
  conflictSeverity: string;
  description?: string;
}

interface LicenseResult {
  serviceId: string;
  conflicts: LicenseConflict[];
  checkedAt: string;
}

interface UpgradeSuggestion {
  packageName: string;
  currentVersion: string;
  suggestedVersion: string;
  severity: string;
  reason?: string;
}

interface UpgradeResult {
  serviceId: string;
  suggestions: UpgradeSuggestion[];
}

interface SbomResult {
  profileId: string;
  serviceId: string;
  generatedAt: string;
  format?: string;
}

// ── Helpers ────────────────────────────────────────────────────────────────

const severityVariant = (
  severity: string,
): 'danger' | 'warning' | 'info' | 'default' | 'success' => {
  const s = severity.toLowerCase();
  if (s === 'critical') return 'danger';
  if (s === 'high') return 'warning';
  if (s === 'medium') return 'info';
  if (s === 'low') return 'default';
  return 'info';
};

// ── Page ───────────────────────────────────────────────────────────────────

/**
 * Página de License Compliance & Upgrades — conformidade de licenças e sugestões
 * de atualização de dependências por serviço.
 *
 * Reforça o pilar de Contract Governance e Source of Truth ao centralizar
 * visibilidade sobre riscos legais e técnicos nas dependências dos serviços.
 */
export function LicenseCompliancePage() {
  const { t } = useTranslation();

  const [serviceId, setServiceId] = useState('');
  const [activeServiceId, setActiveServiceId] = useState('');
  const [activeAction, setActiveAction] = useState<ActiveAction>(null);
  const [sbomResult, setSbomResult] = useState<SbomResult | null>(null);

  // ── License query ────────────────────────────────────────────────────────

  const {
    data: licenseData,
    isLoading: isLicenseLoading,
    isError: isLicenseError,
    refetch: refetchLicense,
  } = useQuery({
    queryKey: ['license-compliance', activeServiceId],
    queryFn: () =>
      client
        .get<LicenseResult>(`/catalog/dependencies/${activeServiceId}/licenses`)
        .then((r) => r.data),
    enabled: activeAction === 'licenses' && activeServiceId.length > 0,
    staleTime: 30_000,
    retry: 1,
  });

  // ── Upgrades query ───────────────────────────────────────────────────────

  const {
    data: upgradesData,
    isLoading: isUpgradesLoading,
    isError: isUpgradesError,
    refetch: refetchUpgrades,
  } = useQuery({
    queryKey: ['dependency-upgrades', activeServiceId],
    queryFn: () =>
      client
        .get<UpgradeResult>(`/catalog/dependencies/${activeServiceId}/upgrades`)
        .then((r) => r.data),
    enabled: activeAction === 'upgrades' && activeServiceId.length > 0,
    staleTime: 30_000,
    retry: 1,
  });

  // ── SBOM mutation ────────────────────────────────────────────────────────

  const sbomMutation = useMutation({
    mutationFn: (svcId: string) =>
      client.post<SbomResult>(`/catalog/dependencies/${svcId}/sbom`, {}).then((r) => r.data),
    onSuccess: (data) => setSbomResult(data),
  });

  // ── Handlers ─────────────────────────────────────────────────────────────

  const handleAction = (action: ActiveAction) => {
    if (!serviceId.trim()) return;
    const svcId = serviceId.trim();
    setActiveServiceId(svcId);
    setActiveAction(action);
    setSbomResult(null);

    if (action === 'sbom') {
      sbomMutation.mutate(svcId);
    }
  };

  const isLoading = isLicenseLoading || isUpgradesLoading || sbomMutation.isPending;

  return (
    <PageContainer>
      <PageHeader
        title={t('licenseCompliance.title')}
        subtitle={t('licenseCompliance.subtitle')}
      />

      {/* ── Section 1: Input & Actions ───────────────────────────────────── */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <FileCheck size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('licenseCompliance.submit')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            <div className="flex flex-col sm:flex-row gap-3 items-end">
              <div className="flex-1">
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('licenseCompliance.serviceId')}
                </label>
                <input
                  type="text"
                  value={serviceId}
                  onChange={(e) => setServiceId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div className="flex gap-2 flex-wrap">
                <button
                  type="button"
                  onClick={() => handleAction('licenses')}
                  disabled={isLoading || !serviceId.trim()}
                  className="px-3 py-2 rounded-md bg-accent text-white text-sm font-medium hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {t('licenseCompliance.checkLicenses')}
                </button>
                <button
                  type="button"
                  onClick={() => handleAction('upgrades')}
                  disabled={isLoading || !serviceId.trim()}
                  className="px-3 py-2 rounded-md border border-edge text-sm font-medium text-heading hover:bg-subtle disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {t('licenseCompliance.getUpgrades')}
                </button>
                <button
                  type="button"
                  onClick={() => handleAction('sbom')}
                  disabled={isLoading || !serviceId.trim()}
                  className="px-3 py-2 rounded-md border border-edge text-sm font-medium text-heading hover:bg-subtle disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {t('licenseCompliance.generateSbom')}
                </button>
              </div>
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* ── Loading state ─────────────────────────────────────────────────── */}
      {isLoading && <PageLoadingState />}

      {/* ── Section 2: License Results ───────────────────────────────────── */}
      {activeAction === 'licenses' && !isLicenseLoading && (
        <PageSection>
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">
                {t('licenseCompliance.licenseConflicts')}
              </h3>
            </CardHeader>
            <CardBody>
              {isLicenseError && (
                <PageErrorState
                  message={t('licenseCompliance.error')}
                  onRetry={() => refetchLicense()}
                />
              )}
              {licenseData && (
                <>
                  {licenseData.conflicts.length === 0 ? (
                    <p className="text-sm text-muted py-4 text-center">
                      {t('licenseCompliance.noConflicts')}
                    </p>
                  ) : (
                    <ul className="space-y-3">
                      {licenseData.conflicts.map((conflict, idx) => (
                        <li
                          key={idx}
                          className="flex items-start justify-between gap-3 rounded-lg border border-edge p-3"
                        >
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-heading">
                              {conflict.packageName}
                            </p>
                            <p className="text-xs text-muted mt-0.5">{conflict.licenseId}</p>
                            {conflict.description && (
                              <p className="text-xs text-body mt-1">{conflict.description}</p>
                            )}
                          </div>
                          <Badge variant={severityVariant(conflict.conflictSeverity)}>
                            {conflict.conflictSeverity}
                          </Badge>
                        </li>
                      ))}
                    </ul>
                  )}
                </>
              )}
            </CardBody>
          </Card>
        </PageSection>
      )}

      {/* ── Section 3: Upgrade Suggestions ───────────────────────────────── */}
      {activeAction === 'upgrades' && !isUpgradesLoading && (
        <PageSection>
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">
                {t('licenseCompliance.upgradeSuggestions')}
              </h3>
            </CardHeader>
            <CardBody>
              {isUpgradesError && (
                <PageErrorState
                  message={t('licenseCompliance.error')}
                  onRetry={() => refetchUpgrades()}
                />
              )}
              {upgradesData && (
                <>
                  {upgradesData.suggestions.length === 0 ? (
                    <p className="text-sm text-muted py-4 text-center">
                      {t('licenseCompliance.noUpgrades')}
                    </p>
                  ) : (
                    <ul className="space-y-3">
                      {upgradesData.suggestions.map((suggestion, idx) => (
                        <li
                          key={idx}
                          className="flex items-center justify-between gap-3 rounded-lg border border-edge p-3"
                        >
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-heading">
                              {suggestion.packageName}
                            </p>
                            <p className="text-xs text-muted mt-0.5">
                              {suggestion.currentVersion}
                              {' → '}
                              <span className="text-success font-medium">
                                {suggestion.suggestedVersion}
                              </span>
                            </p>
                            {suggestion.reason && (
                              <p className="text-xs text-body mt-1">{suggestion.reason}</p>
                            )}
                          </div>
                          <Badge variant={severityVariant(suggestion.severity)}>
                            {suggestion.severity}
                          </Badge>
                        </li>
                      ))}
                    </ul>
                  )}
                </>
              )}
            </CardBody>
          </Card>
        </PageSection>
      )}

      {/* ── Section 4: SBOM ───────────────────────────────────────────────── */}
      {activeAction === 'sbom' && !sbomMutation.isPending && (
        <PageSection>
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">
                {t('licenseCompliance.generateSbom')}
              </h3>
            </CardHeader>
            <CardBody>
              {sbomMutation.isError && (
                <PageErrorState
                  message={t('licenseCompliance.error')}
                  onRetry={() => activeServiceId && sbomMutation.mutate(activeServiceId)}
                />
              )}
              {sbomResult && (
                <div className="rounded-lg border border-success/30 bg-success/5 p-4">
                  <p className="text-sm font-medium text-success mb-2">
                    {t('licenseCompliance.sbomGenerated')}
                  </p>
                  <div className="flex gap-2 items-center text-xs text-muted">
                    <span>{t('licenseCompliance.profileId')}:</span>
                    <span className="font-mono text-heading">{sbomResult.profileId}</span>
                  </div>
                  {sbomResult.generatedAt && (
                    <p className="text-xs text-muted mt-1">
                      {new Date(sbomResult.generatedAt).toLocaleString()}
                    </p>
                  )}
                </div>
              )}
            </CardBody>
          </Card>
        </PageSection>
      )}
    </PageContainer>
  );
}
