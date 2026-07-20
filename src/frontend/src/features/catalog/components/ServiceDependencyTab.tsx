import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Shield } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button, TextArea, Select } from '../../../shared/ui';
import client from '../../../api/client';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ── Types ──────────────────────────────────────────────────────────────────

interface ScanRequest {
  serviceId: string;
  projectFileContent: string;
  projectFileType: string;
}

interface ScanResult {
  profileId: string;
  healthScore: number;
  totalDependencies: number;
  directDependencies: number;
  vulnerabilityCount: number;
}

interface ServiceHealthResult {
  serviceId: string;
  healthScore: number;
  lastScanAt: string;
  totalDeps: number;
  directDeps: number;
  transitiveDeps: number;
  criticalVulnCount: number;
  highVulnCount: number;
  mediumVulnCount: number;
  lowVulnCount: number;
  outdatedCount: number;
  deprecatedCount: number;
  licenseRiskCounts: Record<string, number>;
}

// ── Helpers ────────────────────────────────────────────────────────────────

const healthScoreVariant = (score: number): 'success' | 'warning' | 'danger' => {
  if (score >= 80) return 'success';
  if (score >= 60) return 'warning';
  return 'danger';
};

const healthScoreColor = (score: number): string => {
  if (score >= 80) return 'text-success';
  if (score >= 60) return 'text-warning';
  return 'text-critical';
};

/**
 * Aba de saúde de dependências de um serviço específico, embutida no detalhe do
 * serviço. Substitui a antiga página de portefólio (<c>DependencyDashboardPage</c>),
 * que exigia digitar o serviceId; a vista cross-catálogo "serviços vulneráveis"
 * foi descartada (redundante com o grafo de serviços). Mantém a saúde por serviço
 * (auto-carregada) e o scanner de ficheiro de projeto.
 */
export function ServiceDependencyTab({ serviceId }: { serviceId: string }) {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();

  // Scanner form state
  const [projectFileContent, setProjectFileContent] = useState('');
  const [projectFileType, setProjectFileType] = useState('csproj');
  const [scanResult, setScanResult] = useState<ScanResult | null>(null);

  // ── Service health query (auto-carrega para o serviço atual) ───────────────
  const {
    data: serviceHealth,
    isLoading: isHealthLoading,
    isError: isHealthError,
    refetch: refetchHealth,
  } = useQuery({
    queryKey: ['dependency-health', serviceId, activeEnvironmentId],
    queryFn: () =>
      client
        .get<ServiceHealthResult>(`/catalog/dependencies/${serviceId}/health`)
        .then((r) => r.data),
    enabled: serviceId.length > 0,
    staleTime: 30_000,
    retry: 1,
  });

  // ── Scan mutation ──────────────────────────────────────────────────────────
  const scanMutation = useMutation({
    mutationFn: (body: ScanRequest) =>
      client.post<ScanResult>('/catalog/dependencies/scan', body).then((r) => r.data),
    onSuccess: (data) => {
      setScanResult(data);
      refetchHealth();
    },
  });

  const handleScan = () => {
    if (!projectFileContent.trim()) return;
    scanMutation.mutate({ serviceId, projectFileContent, projectFileType });
  };

  return (
    <div className="space-y-4">
      {/* ── Service Health ────────────────────────────────────────────────── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Shield size={16} className="text-accent" />
            <h3 className="text-sm font-semibold text-heading">
              {t('dependencyDashboard.serviceHealth')}
            </h3>
          </div>
        </CardHeader>
        <CardBody>
          {isHealthLoading && <PageLoadingState />}

          {isHealthError && (
            <PageErrorState
              message={t('dependencyDashboard.error')}
              onRetry={() => refetchHealth()}
            />
          )}

          {serviceHealth && (
            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <span className={`text-3xl font-bold ${healthScoreColor(serviceHealth.healthScore)}`}>
                  {serviceHealth.healthScore}
                </span>
                <Badge variant={healthScoreVariant(serviceHealth.healthScore)}>
                  {t('dependencyDashboard.healthScore')}
                </Badge>
                <span className="text-xs text-muted ml-auto">
                  {t('dependencyDashboard.lastScanAt')}:{' '}
                  {new Date(serviceHealth.lastScanAt).toLocaleString()}
                </span>
              </div>

              {/* Vulnerability breakdown */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <div className="rounded-lg border border-edge bg-critical/5 p-3 text-center">
                  <p className="text-2xl font-bold text-critical">{serviceHealth.criticalVulnCount}</p>
                  <p className="text-xs text-muted mt-1">{t('dependencyDashboard.criticalVulns')}</p>
                </div>
                {/* text-orange-500 / bg-orange-500/5: taxonomia intencional — nível "high" de
                    severidade fica entre critical e warning; não existe token --color-orange ainda. */}
                <div className="rounded-lg border border-edge bg-orange-500/5 p-3 text-center">
                  <p className="text-2xl font-bold text-orange-500">{serviceHealth.highVulnCount}</p>
                  <p className="text-xs text-muted mt-1">{t('dependencyDashboard.highVulns')}</p>
                </div>
                <div className="rounded-lg border border-edge bg-warning/5 p-3 text-center">
                  <p className="text-2xl font-bold text-warning">{serviceHealth.mediumVulnCount}</p>
                  <p className="text-xs text-muted mt-1">{t('dependencyDashboard.mediumVulns')}</p>
                </div>
                <div className="rounded-lg border border-edge bg-info/5 p-3 text-center">
                  <p className="text-2xl font-bold text-info">{serviceHealth.lowVulnCount}</p>
                  <p className="text-xs text-muted mt-1">{t('dependencyDashboard.lowVulns')}</p>
                </div>
              </div>

              {/* Outdated / Deprecated */}
              <div className="flex gap-6 text-sm">
                <div>
                  <span className="text-muted">{t('dependencyDashboard.outdatedCount')}: </span>
                  <span className="font-semibold text-heading">{serviceHealth.outdatedCount}</span>
                </div>
                <div>
                  <span className="text-muted">{t('dependencyDashboard.deprecatedCount')}: </span>
                  <span className="font-semibold text-heading">{serviceHealth.deprecatedCount}</span>
                </div>
              </div>

              {/* License risk */}
              {Object.keys(serviceHealth.licenseRiskCounts).length > 0 && (
                <div>
                  <p className="text-xs font-medium text-muted mb-2">
                    {t('dependencyDashboard.licenseRiskCounts')}
                  </p>
                  <ul className="space-y-1">
                    {Object.entries(serviceHealth.licenseRiskCounts).map(([risk, count]) => (
                      <li key={risk} className="flex justify-between text-sm">
                        <span className="text-body">{risk}</span>
                        <span className="font-semibold text-heading">{count}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </CardBody>
      </Card>

      {/* ── Scanner ───────────────────────────────────────────────────────── */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-semibold text-heading">
            {t('dependencyDashboard.scanner')}
          </h3>
        </CardHeader>
        <CardBody>
          <div className="space-y-4">
            <TextArea
              label={t('dependencyDashboard.projectFileContent')}
              value={projectFileContent}
              onChange={(e) => setProjectFileContent(e.target.value)}
              rows={6}
              textareaClassName="font-mono resize-y"
            />
            <div className="flex items-end gap-4">
              <div className="w-48">
                <Select
                  label={t('dependencyDashboard.projectFileType')}
                  value={projectFileType}
                  onChange={(e) => setProjectFileType(e.target.value)}
                  options={[
                    { value: 'csproj', label: 'csproj' },
                    { value: 'package.json', label: 'package.json' },
                    { value: 'pom.xml', label: 'pom.xml' },
                  ]}
                />
              </div>
              <Button
                variant="primary"
                onClick={handleScan}
                disabled={scanMutation.isPending || !projectFileContent.trim()}
                loading={scanMutation.isPending}
              >
                {scanMutation.isPending
                  ? t('dependencyDashboard.loading')
                  : t('dependencyDashboard.scanButton')}
              </Button>
            </div>

            {scanMutation.isError && (
              <p className="text-sm text-critical">{t('dependencyDashboard.scanError')}</p>
            )}

            {scanResult && (
              <div className="mt-4 p-4 rounded-lg border border-edge bg-subtle">
                <p className="text-xs font-medium text-muted mb-3">
                  {t('dependencyDashboard.scanSuccess')}
                </p>
                <div className="flex flex-wrap gap-6 items-center">
                  <div className="flex flex-col items-center">
                    <span className={`text-4xl font-bold ${healthScoreColor(scanResult.healthScore)}`}>
                      {scanResult.healthScore}
                    </span>
                    <Badge variant={healthScoreVariant(scanResult.healthScore)} className="mt-1">
                      {t('dependencyDashboard.healthScore')}
                    </Badge>
                  </div>
                  <div className="flex gap-6 text-center">
                    <div>
                      <p className="text-2xl font-semibold text-heading">{scanResult.totalDependencies}</p>
                      <p className="text-xs text-muted">{t('dependencyDashboard.totalDependencies')}</p>
                    </div>
                    <div>
                      <p className="text-2xl font-semibold text-heading">{scanResult.directDependencies}</p>
                      <p className="text-xs text-muted">{t('dependencyDashboard.directDependencies')}</p>
                    </div>
                    <div>
                      <p className="text-2xl font-semibold text-heading">{scanResult.vulnerabilityCount}</p>
                      <p className="text-xs text-muted">{t('dependencyDashboard.vulnerabilityCount')}</p>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
