import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Shield } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
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

interface VulnerableService {
  profileId: string;
  serviceId: string;
  healthScore: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lastScanAt: string;
}

// ── Helpers ────────────────────────────────────────────────────────────────

const healthScoreVariant = (score: number): 'success' | 'warning' | 'danger' => {
  if (score >= 80) return 'success';
  if (score >= 60) return 'warning';
  return 'danger';
};

const healthScoreColor = (score: number): string => {
  if (score >= 80) return 'text-green-600 dark:text-green-400';
  if (score >= 60) return 'text-yellow-600 dark:text-yellow-400';
  return 'text-red-600 dark:text-red-400';
};

// ── Page ───────────────────────────────────────────────────────────────────

/**
 * Página de Dependency Dashboard — saúde de dependências por serviço.
 *
 * Oferece scanner de dependências com scoring de saúde, consulta de perfil
 * de vulnerabilidades por serviço e listagem de serviços em risco.
 * Reforça o pilar de Change Intelligence e Production Change Confidence.
 */
export function DependencyDashboardPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();

  // Scanner form state
  const [scanServiceId, setScanServiceId] = useState('');
  const [projectFileContent, setProjectFileContent] = useState('');
  const [projectFileType, setProjectFileType] = useState('csproj');
  const [scanResult, setScanResult] = useState<ScanResult | null>(null);

  // Service health form state
  const [healthServiceId, setHealthServiceId] = useState('');
  const [fetchHealthId, setFetchHealthId] = useState('');

  // Vulnerable list toggle
  const [loadVulnerable, setLoadVulnerable] = useState(false);

  // ── Scan mutation ────────────────────────────────────────────────────────

  const scanMutation = useMutation({
    mutationFn: (body: ScanRequest) =>
      client.post<ScanResult>('/catalog/dependencies/scan', body).then((r) => r.data),
    onSuccess: (data) => setScanResult(data),
  });

  // ── Service health query ─────────────────────────────────────────────────

  const {
    data: serviceHealth,
    isLoading: isHealthLoading,
    isError: isHealthError,
    refetch: refetchHealth,
  } = useQuery({
    queryKey: ['dependency-health', fetchHealthId, activeEnvironmentId],
    queryFn: () =>
      client
        .get<ServiceHealthResult>(`/catalog/dependencies/${fetchHealthId}/health`)
        .then((r) => r.data),
    enabled: fetchHealthId.length > 0,
    staleTime: 30_000,
    retry: 1,
  });

  // ── Vulnerable services query ────────────────────────────────────────────

  const {
    data: vulnerableServices,
    isLoading: isVulnerableLoading,
    isError: isVulnerableError,
    refetch: refetchVulnerable,
  } = useQuery({
    queryKey: ['dependency-vulnerable', activeEnvironmentId],
    queryFn: () =>
      client.get<VulnerableService[]>('/catalog/dependencies/vulnerable').then((r) => r.data),
    enabled: loadVulnerable,
    staleTime: 30_000,
    retry: 1,
  });

  // ── Handlers ─────────────────────────────────────────────────────────────

  const handleScan = () => {
    if (!scanServiceId.trim() || !projectFileContent.trim()) return;
    scanMutation.mutate({
      serviceId: scanServiceId.trim(),
      projectFileContent,
      projectFileType,
    });
  };

  const handleFetchHealth = () => {
    if (healthServiceId.trim()) {
      setFetchHealthId(healthServiceId.trim());
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('dependencyDashboard.title')}
        subtitle={t('dependencyDashboard.subtitle')}
      />

      {/* ── Section 1: Scanner ────────────────────────────────────────────── */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('dependencyDashboard.scanner')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            <div className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('dependencyDashboard.serviceId')}
                </label>
                <input
                  type="text"
                  value={scanServiceId}
                  onChange={(e) => setScanServiceId(e.target.value)}
                  placeholder={t('dependencyDashboard.serviceIdPlaceholder', 'Enter service name or ID')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('dependencyDashboard.projectFileContent')}
                </label>
                <textarea
                  value={projectFileContent}
                  onChange={(e) => setProjectFileContent(e.target.value)}
                  rows={6}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono resize-y"
                />
              </div>
              <div className="flex items-end gap-4">
                <div className="w-48">
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('dependencyDashboard.projectFileType')}
                  </label>
                  <select
                    value={projectFileType}
                    onChange={(e) => setProjectFileType(e.target.value)}
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  >
                    <option value="csproj">csproj</option>
                    <option value="package.json">package.json</option>
                    <option value="pom.xml">pom.xml</option>
                  </select>
                </div>
                <button
                  type="button"
                  onClick={handleScan}
                  disabled={scanMutation.isPending || !scanServiceId.trim() || !projectFileContent.trim()}
                  className="px-4 py-2 rounded-md bg-accent text-white text-sm font-medium hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {scanMutation.isPending
                    ? t('dependencyDashboard.loading')
                    : t('dependencyDashboard.scanButton')}
                </button>
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
                      <span
                        className={`text-4xl font-bold ${healthScoreColor(scanResult.healthScore)}`}
                      >
                        {scanResult.healthScore}
                      </span>
                      <Badge variant={healthScoreVariant(scanResult.healthScore)} className="mt-1">
                        {t('dependencyDashboard.healthScore')}
                      </Badge>
                    </div>
                    <div className="flex gap-6 text-center">
                      <div>
                        <p className="text-2xl font-semibold text-heading">
                          {scanResult.totalDependencies}
                        </p>
                        <p className="text-xs text-muted">
                          {t('dependencyDashboard.totalDependencies')}
                        </p>
                      </div>
                      <div>
                        <p className="text-2xl font-semibold text-heading">
                          {scanResult.directDependencies}
                        </p>
                        <p className="text-xs text-muted">
                          {t('dependencyDashboard.directDependencies')}
                        </p>
                      </div>
                      <div>
                        <p className="text-2xl font-semibold text-heading">
                          {scanResult.vulnerabilityCount}
                        </p>
                        <p className="text-xs text-muted">
                          {t('dependencyDashboard.vulnerabilityCount')}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* ── Section 2: Service Health ─────────────────────────────────────── */}
      <PageSection>
        <Card>
          <CardHeader>
            <h3 className="text-sm font-semibold text-heading">
              {t('dependencyDashboard.serviceHealth')}
            </h3>
          </CardHeader>
          <CardBody>
            <div className="flex gap-3 items-end mb-4">
              <div className="flex-1">
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('dependencyDashboard.serviceId')}
                </label>
                <input
                  type="text"
                  value={healthServiceId}
                  onChange={(e) => setHealthServiceId(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleFetchHealth()}
                  placeholder={t('dependencyDashboard.serviceIdPlaceholder', 'Enter service name or ID')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <button
                type="button"
                onClick={handleFetchHealth}
                disabled={!healthServiceId.trim()}
                className="px-4 py-2 rounded-md bg-accent text-white text-sm font-medium hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {t('dependencyDashboard.serviceHealth')}
              </button>
            </div>

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
                  <span
                    className={`text-3xl font-bold ${healthScoreColor(serviceHealth.healthScore)}`}
                  >
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
                    <p className="text-2xl font-bold text-critical">
                      {serviceHealth.criticalVulnCount}
                    </p>
                    <p className="text-xs text-muted mt-1">{t('dependencyDashboard.criticalVulns')}</p>
                  </div>
                  <div className="rounded-lg border border-edge bg-orange-500/5 p-3 text-center">
                    <p className="text-2xl font-bold text-orange-500">
                      {serviceHealth.highVulnCount}
                    </p>
                    <p className="text-xs text-muted mt-1">{t('dependencyDashboard.highVulns')}</p>
                  </div>
                  <div className="rounded-lg border border-edge bg-warning/5 p-3 text-center">
                    <p className="text-2xl font-bold text-warning">
                      {serviceHealth.mediumVulnCount}
                    </p>
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
      </PageSection>

      {/* ── Section 3: Vulnerable Services ───────────────────────────────── */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-semibold text-heading">
                {t('dependencyDashboard.vulnerableServices')}
              </h3>
              <button
                type="button"
                onClick={() => {
                  setLoadVulnerable(true);
                  if (loadVulnerable) refetchVulnerable();
                }}
                className="px-3 py-1.5 rounded-md bg-accent text-white text-xs font-medium hover:bg-accent-hover transition-colors"
              >
                {isVulnerableLoading
                  ? t('dependencyDashboard.loading')
                  : t('dependencyDashboard.loadVulnerable')}
              </button>
            </div>
          </CardHeader>
          <CardBody>
            {isVulnerableError && (
              <PageErrorState
                message={t('dependencyDashboard.error')}
                onRetry={() => refetchVulnerable()}
              />
            )}

            {loadVulnerable && !isVulnerableLoading && !isVulnerableError && (
              <>
                {!vulnerableServices || vulnerableServices.length === 0 ? (
                  <p className="text-sm text-muted py-4 text-center">
                    {t('dependencyDashboard.noVulnerable')}
                  </p>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-edge">
                          <th className="py-2 px-3 text-left text-xs font-medium text-muted">
                            {t('dependencyDashboard.serviceId')}
                          </th>
                          <th className="py-2 px-3 text-left text-xs font-medium text-muted">
                            {t('dependencyDashboard.healthScore')}
                          </th>
                          <th className="py-2 px-3 text-left text-xs font-medium text-muted">
                            {t('dependencyDashboard.criticalVulns')}
                          </th>
                          <th className="py-2 px-3 text-left text-xs font-medium text-muted">
                            {t('dependencyDashboard.highVulns')}
                          </th>
                          <th className="py-2 px-3 text-left text-xs font-medium text-muted">
                            {t('dependencyDashboard.mediumVulns')}
                          </th>
                          <th className="py-2 px-3 text-left text-xs font-medium text-muted">
                            {t('dependencyDashboard.lastScanAt')}
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {vulnerableServices.map((svc) => (
                          <tr
                            key={svc.profileId}
                            className="border-b border-edge last:border-0 hover:bg-subtle transition-colors"
                          >
                            <td className="py-2 px-3 font-mono text-xs text-heading">
                              {svc.serviceId}
                            </td>
                            <td className="py-2 px-3">
                              <Badge variant={healthScoreVariant(svc.healthScore)}>
                                {svc.healthScore}
                              </Badge>
                            </td>
                            <td className="py-2 px-3 text-critical font-semibold">
                              {svc.criticalCount}
                            </td>
                            <td className="py-2 px-3 text-orange-500 font-semibold">
                              {svc.highCount}
                            </td>
                            <td className="py-2 px-3 text-warning font-semibold">
                              {svc.mediumCount}
                            </td>
                            <td className="py-2 px-3 text-muted text-xs">
                              {new Date(svc.lastScanAt).toLocaleString()}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </>
            )}
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}
