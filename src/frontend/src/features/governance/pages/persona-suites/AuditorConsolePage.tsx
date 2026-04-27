import { useTranslation } from 'react-i18next';
import { Shield, FileText, Search, AlertTriangle, CheckCircle2, Download, Lock } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';

const COMPLIANCE_STANDARDS = [
  { key: 'soc2', label: 'SOC 2 Type II', href: '/governance/compliance?standard=soc2', status: 'compliant' },
  { key: 'iso27001', label: 'ISO/IEC 27001:2022', href: '/governance/compliance?standard=iso27001', status: 'compliant' },
  { key: 'pci-dss', label: 'PCI-DSS v4.0', href: '/governance/compliance?standard=pci-dss', status: 'partial' },
  { key: 'hipaa', label: 'HIPAA Security Rule', href: '/governance/compliance?standard=hipaa', status: 'compliant' },
  { key: 'gdpr', label: 'GDPR', href: '/governance/compliance?standard=gdpr', status: 'partial' },
  { key: 'cmmc', label: 'CMMC 2.0 Level 2', href: '/governance/compliance?standard=cmmc', status: 'not-assessed' },
  { key: 'fedramp', label: 'FedRAMP Moderate', href: '/governance/compliance?standard=fedramp', status: 'not-assessed' },
  { key: 'nis2', label: 'NIS2', href: '/governance/compliance?standard=nis2', status: 'compliant' },
];

const STATUS_VARIANT = {
  compliant: 'success',
  partial: 'warning',
  'not-assessed': 'secondary',
} as const;

export function AuditorConsolePage() {
  const { t } = useTranslation();

  return (
    <PageContainer>
      <PageHeader
        title={t('personaSuite.auditor.title')}
        subtitle={t('personaSuite.auditor.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button size="sm" variant="ghost">
              <Search size={14} className="mr-1" />
              {t('auditorConsole.searchAudit')}
            </Button>
            <Button size="sm" variant="ghost">
              <Download size={14} className="mr-1" />
              {t('auditorConsole.exportSigned')}
            </Button>
          </div>
        }
      />
      <PageSection>
        {/* Compliance Standards */}
        <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
          <Shield size={14} />
          {t('auditorConsole.complianceStandards')}
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3 mb-6">
          {COMPLIANCE_STANDARDS.map((std) => (
            <Link key={std.key} to={std.href}>
              <Card className="hover:border-accent/60 transition-colors">
                <CardBody className="p-3">
                  <div className="flex items-start justify-between gap-2">
                    <p className="text-xs font-medium">{std.label}</p>
                    <Badge variant={STATUS_VARIANT[std.status as keyof typeof STATUS_VARIANT] ?? 'secondary'} className="text-xs shrink-0">
                      {std.status === 'compliant' ? <CheckCircle2 size={8} className="mr-1" /> : <AlertTriangle size={8} className="mr-1" />}
                      {t(`auditorConsole.status.${std.status}`, { defaultValue: std.status })}
                    </Badge>
                  </div>
                </CardBody>
              </Card>
            </Link>
          ))}
        </div>

        {/* Audit Links */}
        <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
          <FileText size={14} />
          {t('auditorConsole.auditSurfaces')}
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {[
            { label: t('auditorConsole.evidencePacks'), icon: <FileText size={14} />, to: '/governance/evidence', color: 'text-accent' },
            { label: t('auditorConsole.auditTrail'), icon: <Search size={14} />, to: '/audit-compliance/audit', color: 'text-info' },
            { label: t('auditorConsole.breakGlassHistory'), icon: <Lock size={14} />, to: '/identity-access/break-glass', color: 'text-warning' },
            { label: t('auditorConsole.accessReviews'), icon: <CheckCircle2 size={14} />, to: '/identity-access/access-reviews', color: 'text-success' },
            { label: t('auditorConsole.policyViolations'), icon: <AlertTriangle size={14} />, to: '/governance/risk', color: 'text-destructive' },
            { label: t('auditorConsole.evidenceIntegrity'), icon: <Shield size={14} />, to: '/governance/evidence', color: 'text-primary' },
          ].map((item) => (
            <Link key={item.to} to={item.to}>
              <Card className="hover:border-accent/60 transition-colors">
                <CardBody className="p-3 flex items-center gap-2">
                  <span className={item.color}>{item.icon}</span>
                  <span className="text-sm font-medium">{item.label}</span>
                </CardBody>
              </Card>
            </Link>
          ))}
        </div>

        <div className="mt-6 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
          {t('personaSuite.simulatedBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
