import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { FileCheck } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { Button } from '../../../shared/ui';

// ── Types ──────────────────────────────────────────────────────────────────

type ActiveAction = 'licenses' | 'upgrades' | 'sbom';

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

/**
 * Aba de conformidade de licenças e SBOM de um serviço específico, embutida no
 * detalhe do serviço. Substitui a antiga página de portefólio
 * (<c>LicenseCompliancePage</c>), que exigia digitar o serviceId manualmente —
 * aqui o serviço já é conhecido pelo contexto.
 */
export function ServiceLicenseComplianceTab({ serviceId }: { serviceId: string }) {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();

  const [activeAction, setActiveAction] = useState<ActiveAction>('licenses');
  const [sbomResult, setSbomResult] = useState<SbomResult | null>(null);

  // ── License query ────────────────────────────────────────────────────────
  const {
    data: licenseData,
    isLoading: isLicenseLoading,
    isError: isLicenseError,
    refetch: refetchLicense,
  } = useQuery({
    queryKey: ['license-compliance', serviceId, activeEnvironmentId],
    queryFn: () =>
      client
        .get<LicenseResult>(`/catalog/dependencies/${serviceId}/licenses`)
        .then((r) => r.data),
    enabled: activeAction === 'licenses' && serviceId.length > 0,
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
    queryKey: ['dependency-upgrades', serviceId, activeEnvironmentId],
    queryFn: () =>
      client
        .get<UpgradeResult>(`/catalog/dependencies/${serviceId}/upgrades`)
        .then((r) => r.data),
    enabled: activeAction === 'upgrades' && serviceId.length > 0,
    staleTime: 30_000,
    retry: 1,
  });

  // ── SBOM mutation ────────────────────────────────────────────────────────
  const sbomMutation = useMutation({
    mutationFn: () =>
      client.post<SbomResult>(`/catalog/dependencies/${serviceId}/sbom`, {}).then((r) => r.data),
    onSuccess: (data) => setSbomResult(data),
  });

  const handleAction = (action: ActiveAction) => {
    setActiveAction(action);
    if (action === 'sbom') {
      setSbomResult(null);
      sbomMutation.mutate();
    }
  };

  const isLoading = isLicenseLoading || isUpgradesLoading || sbomMutation.isPending;

  return (
    <div className="space-y-4">
      {/* ── Actions ─────────────────────────────────────────────────────── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <FileCheck size={16} className="text-accent" />
            <h3 className="text-sm font-semibold text-heading">
              {t('licenseCompliance.title')}
            </h3>
          </div>
        </CardHeader>
        <CardBody>
          <div className="flex gap-2 flex-wrap">
            <Button
              variant={activeAction === 'licenses' ? 'primary' : 'outline'}
              size="sm"
              onClick={() => handleAction('licenses')}
              disabled={isLoading}
            >
              {t('licenseCompliance.checkLicenses')}
            </Button>
            <Button
              variant={activeAction === 'upgrades' ? 'primary' : 'outline'}
              size="sm"
              onClick={() => handleAction('upgrades')}
              disabled={isLoading}
            >
              {t('licenseCompliance.getUpgrades')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => handleAction('sbom')}
              disabled={isLoading}
            >
              {t('licenseCompliance.generateSbom')}
            </Button>
          </div>
        </CardBody>
      </Card>

      {/* ── Loading state ─────────────────────────────────────────────────── */}
      {isLoading && <PageLoadingState />}

      {/* ── License Results ───────────────────────────────────────────────── */}
      {activeAction === 'licenses' && !isLicenseLoading && (
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
      )}

      {/* ── Upgrade Suggestions ───────────────────────────────────────────── */}
      {activeAction === 'upgrades' && !isUpgradesLoading && (
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
      )}

      {/* ── SBOM ──────────────────────────────────────────────────────────── */}
      {activeAction === 'sbom' && !sbomMutation.isPending && (
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
                onRetry={() => sbomMutation.mutate()}
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
      )}
    </div>
  );
}
