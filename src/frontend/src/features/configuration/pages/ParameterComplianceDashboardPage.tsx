import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import {
  ShieldCheck, CheckCircle, AlertTriangle, XCircle, Eye, Lock, RefreshCw,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageHeader } from '../../../components/PageHeader';

/**
 * ParameterComplianceDashboardPage — dashboard executivo de compliance de parametrização.
 * Mostra cobertura i18n, parâmetros depreciados, validação e distribuição por categoria.
 * Pilar: Governance, Operational Consistency.
 * Persona: Platform Admin, Executive, Auditor.
 */

interface CategoryCompliance {
  category: string;
  total: number;
  withI18n: number;
  deprecated: number;
}

interface ComplianceSummary {
  totalDefinitions: number;
  withI18nKeys: number;
  withoutI18nKeys: number;
  i18nCoveragePercent: number;
  deprecatedCount: number;
  sensitiveCount: number;
  withValidationRules: number;
  withoutValidationRules: number;
  validationCoveragePercent: number;
  editableCount: number;
  readOnlyCount: number;
  byCategory: CategoryCompliance[];
  deprecatedKeys: string[];
}

export function ParameterComplianceDashboardPage() {
  const { t } = useTranslation();
  const [summary, setSummary] = useState<ComplianceSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  const fetchSummary = async () => {
    setLoading(true);
    setError(false);
    try {
      const resp = await fetch('/api/v1/configuration/analytics/compliance');
      if (resp.ok) setSummary(await resp.json());
      else setError(true);
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void fetchSummary(); }, []);

  const getComplianceColor = (pct: number) => {
    if (pct >= 95) return 'text-green-400';
    if (pct >= 80) return 'text-amber-400';
    return 'text-red-400';
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('phase5.compliance.title', 'Parameterization Compliance Dashboard')}
        subtitle={t('phase5.compliance.subtitle', 'Executive view of configuration governance and compliance')}
        icon={<ShieldCheck size={24} />}
        actions={
          <button
            onClick={() => void fetchSummary()}
            className="flex items-center gap-2 px-3 py-2 rounded-lg bg-slate-700 hover:bg-slate-600 text-sm"
          >
            <RefreshCw size={14} />
            {t('phase5.compliance.refresh', 'Refresh')}
          </button>
        }
      />

      {loading && (
        <div className="text-center py-12 text-slate-400">
          {t('phase5.compliance.loading', 'Loading compliance summary...')}
        </div>
      )}

      {!loading && error && (
        <PageErrorState />
      )}

      {!loading && !error && summary && (
        <>
          {/* Compliance Score Cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mt-6">
            <Card>
              <CardBody className="text-center">
                <CheckCircle size={20} className={`mx-auto mb-2 ${getComplianceColor(summary.i18nCoveragePercent)}`} />
                <div className={`text-2xl font-bold ${getComplianceColor(summary.i18nCoveragePercent)}`}>
                  {summary.i18nCoveragePercent}%
                </div>
                <div className="text-xs text-slate-400">{t('phase5.compliance.i18nCoverage', 'i18n Coverage')}</div>
                <div className="text-xs text-slate-500 mt-1">
                  {summary.withI18nKeys}/{summary.totalDefinitions}
                </div>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <Eye size={20} className={`mx-auto mb-2 ${getComplianceColor(summary.validationCoveragePercent)}`} />
                <div className={`text-2xl font-bold ${getComplianceColor(summary.validationCoveragePercent)}`}>
                  {summary.validationCoveragePercent}%
                </div>
                <div className="text-xs text-slate-400">{t('phase5.compliance.validationCoverage', 'Validation Coverage')}</div>
                <div className="text-xs text-slate-500 mt-1">
                  {summary.withValidationRules}/{summary.totalDefinitions}
                </div>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <AlertTriangle size={20} className="mx-auto mb-2 text-amber-400" />
                <div className="text-2xl font-bold text-amber-400">{summary.deprecatedCount}</div>
                <div className="text-xs text-slate-400">{t('phase5.compliance.deprecated', 'Deprecated Parameters')}</div>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <Lock size={20} className="mx-auto mb-2 text-red-400" />
                <div className="text-2xl font-bold">{summary.sensitiveCount}</div>
                <div className="text-xs text-slate-400">{t('phase5.compliance.sensitive', 'Sensitive Parameters')}</div>
              </CardBody>
            </Card>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
            {/* Category Distribution */}
            <Card>
              <CardHeader className="flex items-center gap-2">
                <ShieldCheck size={18} />
                <span>{t('phase5.compliance.byCategory', 'Compliance by Category')}</span>
              </CardHeader>
              <CardBody>
                <div className="space-y-2">
                  {summary.byCategory.map((cat) => {
                    const i18nPct = cat.total > 0 ? Math.round((cat.withI18n / cat.total) * 100) : 0;
                    return (
                      <div key={cat.category} className="flex items-center justify-between py-2 border-b border-slate-700/40">
                        <div>
                          <span className="text-sm font-medium">{cat.category}</span>
                          <span className="text-xs text-slate-500 ml-2">({cat.total} {t('phase5.compliance.params', 'params')})</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <Badge variant={i18nPct === 100 ? 'success' : 'warning'}>{i18nPct}% i18n</Badge>
                          {cat.deprecated > 0 && (
                            <Badge variant="danger">{cat.deprecated} {t('phase5.compliance.deprecatedShort', 'dep.')}</Badge>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </CardBody>
            </Card>

            {/* Editable vs Read-Only */}
            <Card>
              <CardHeader className="flex items-center gap-2">
                <Lock size={18} />
                <span>{t('phase5.compliance.editability', 'Editability Overview')}</span>
              </CardHeader>
              <CardBody>
                <div className="space-y-4">
                  <div className="flex justify-between items-center">
                    <span className="text-sm">{t('phase5.compliance.editable', 'Editable')}</span>
                    <span className="text-lg font-bold text-green-400">{summary.editableCount}</span>
                  </div>
                  <div className="w-full bg-slate-700 rounded-full h-2">
                    <div
                      className="bg-green-500 h-2 rounded-full"
                      style={{ width: `${summary.totalDefinitions > 0 ? (summary.editableCount / summary.totalDefinitions * 100) : 0}%` }}
                    />
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-sm">{t('phase5.compliance.readOnly', 'Read-Only')}</span>
                    <span className="text-lg font-bold text-slate-400">{summary.readOnlyCount}</span>
                  </div>
                </div>

                {/* Deprecated Keys */}
                {summary.deprecatedKeys.length > 0 && (
                  <div className="mt-6">
                    <h4 className="text-sm font-semibold text-amber-400 mb-2">
                      {t('phase5.compliance.deprecatedList', 'Deprecated Parameters')}
                    </h4>
                    <div className="space-y-1 max-h-40 overflow-auto">
                      {summary.deprecatedKeys.map((key) => (
                        <div key={key} className="flex items-center gap-2 text-xs">
                          <XCircle size={12} className="text-amber-400" />
                          <code className="text-slate-300">{key}</code>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </CardBody>
            </Card>
          </div>
        </>
      )}
    </PageContainer>
  );
}

export default ParameterComplianceDashboardPage;
