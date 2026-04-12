/**
 * Tab "Health & Troubleshooting" da AdvancedConfigurationConsolePage.
 *
 * Apresenta checks de saúde da plataforma de configuração, estatísticas
 * de governança de definições e breakdown por domínio.
 */
import { memo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Activity,
  Shield,
  CheckCircle2,
  AlertTriangle,
  XCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { DOMAINS, matchDomain } from './AdvancedConfigConsoleTypes';
import type { ConfigurationDefinitionDto } from '../types';

// ── Types ──────────────────────────────────────────────────────────────

export interface HealthCheck {
  name: string;
  status: 'ok' | 'warning' | 'error';
  message: string;
}

// ── Props ──────────────────────────────────────────────────────────────

export interface AdvancedConfigHealthTabProps {
  healthChecks: HealthCheck[];
  definitions: ConfigurationDefinitionDto[] | undefined;
}

// ── Component ──────────────────────────────────────────────────────────

export const AdvancedConfigHealthTab = memo(function AdvancedConfigHealthTab({
  healthChecks,
  definitions,
}: AdvancedConfigHealthTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Activity className="w-5 h-5 text-brand-600" />
            <h3 className="font-semibold">{t('advancedConfig.health.title', 'Configuration Platform Health')}</h3>
          </div>
        </CardHeader>
        <CardBody>
          <div className="space-y-3">
            {healthChecks.map((check, idx) => (
              <div key={idx} className="flex items-center justify-between p-3 bg-subtle rounded-lg">
                <div className="flex items-center gap-3">
                  {check.status === 'ok' && <CheckCircle2 className="w-5 h-5 text-success" />}
                  {check.status === 'warning' && <AlertTriangle className="w-5 h-5 text-warning" />}
                  {check.status === 'error' && <XCircle className="w-5 h-5 text-critical" />}
                  <span className="text-sm font-medium">{check.name}</span>
                </div>
                <span className="text-xs text-faded">{check.message}</span>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Shield className="w-5 h-5 text-brand-600" />
            <h3 className="font-semibold">{t('advancedConfig.health.governanceTitle', 'Definition Governance')}</h3>
          </div>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="text-center p-4 bg-subtle rounded-lg">
              <p className="text-2xl font-bold text-brand-600">{definitions?.length ?? 0}</p>
              <p className="text-xs text-faded">{t('advancedConfig.health.totalDefinitions', 'Total Definitions')}</p>
            </div>
            <div className="text-center p-4 bg-subtle rounded-lg">
              <p className="text-2xl font-bold text-warning">{definitions?.filter((d: ConfigurationDefinitionDto) => d.isSensitive).length ?? 0}</p>
              <p className="text-xs text-faded">{t('advancedConfig.health.sensitiveDefinitions', 'Sensitive')}</p>
            </div>
            <div className="text-center p-4 bg-subtle rounded-lg">
              <p className="text-2xl font-bold text-success">{definitions?.filter((d: ConfigurationDefinitionDto) => d.isEditable).length ?? 0}</p>
              <p className="text-xs text-faded">{t('advancedConfig.health.editableDefinitions', 'Editable')}</p>
            </div>
            <div className="text-center p-4 bg-subtle rounded-lg">
              <p className="text-2xl font-bold text-info">{definitions?.filter((d: ConfigurationDefinitionDto) => !d.isInheritable).length ?? 0}</p>
              <p className="text-xs text-faded">{t('advancedConfig.health.mandatoryDefinitions', 'Mandatory (System-only)')}</p>
            </div>
          </div>

          <div className="mt-6">
            <h4 className="text-sm font-medium text-body mb-3">
              {t('advancedConfig.health.domainBreakdown', 'Domain Breakdown')}
            </h4>
            <div className="space-y-2">
              {DOMAINS.filter(d => d.key !== 'all').map(domain => {
                const count = definitions?.filter((def: ConfigurationDefinitionDto) => matchDomain(def.key, domain)).length ?? 0;
                return (
                  <div key={domain.key} className="flex items-center justify-between py-2 px-3 bg-subtle rounded">
                    <div className="flex items-center gap-2">
                      {domain.icon}
                      <span className="text-sm">{t(`advancedConfig.domains.${domain.key}`, domain.key)}</span>
                    </div>
                    <Badge variant="info">{count}</Badge>
                  </div>
                );
              })}
            </div>
          </div>
        </CardBody>
      </Card>
    </div>
  );
});
