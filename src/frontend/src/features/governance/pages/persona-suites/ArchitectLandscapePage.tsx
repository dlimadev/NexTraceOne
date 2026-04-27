import { useTranslation } from 'react-i18next';
import { Network, GitBranch, Shield, BarChart3, AlertTriangle, BookOpen } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';

const SECTIONS = [
  { key: 'topology', label: 'architecture.topology', icon: <Network size={16} />, to: '/catalog/topology', color: 'text-accent', description: 'architecture.topologyDesc' },
  { key: 'dependency-risk', label: 'architecture.dependencyRisk', icon: <AlertTriangle size={16} />, to: '/catalog/dependencies', color: 'text-warning', description: 'architecture.dependencyRiskDesc' },
  { key: 'coupling', label: 'architecture.serviceCoupling', icon: <GitBranch size={16} />, to: '/catalog/services', color: 'text-info', description: 'architecture.couplingDesc' },
  { key: 'contract-adoption', label: 'architecture.contractAdoption', icon: <BookOpen size={16} />, to: '/contracts', color: 'text-success', description: 'architecture.contractAdoptionDesc' },
  { key: 'breaking-changes', label: 'architecture.breakingChanges', icon: <Shield size={16} />, to: '/contracts/breaking-changes', color: 'text-destructive', description: 'architecture.breakingChangesDesc' },
  { key: 'maturity', label: 'architecture.maturityDistribution', icon: <BarChart3 size={16} />, to: '/governance/maturity', color: 'text-primary', description: 'architecture.maturityDesc' },
];

export function ArchitectLandscapePage() {
  const { t } = useTranslation();

  return (
    <PageContainer>
      <PageHeader
        title={t('personaSuite.architect.title')}
        subtitle={t('personaSuite.architect.subtitle')}
        actions={
          <Badge variant="secondary">{t('personaSuite.architect.role')}</Badge>
        }
      />
      <PageSection>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {SECTIONS.map((s) => (
            <Link key={s.key} to={s.to}>
              <Card className="hover:border-accent/60 transition-colors h-full">
                <CardBody className="p-4">
                  <div className={`flex items-center gap-2 mb-2 ${s.color}`}>
                    {s.icon}
                    <h3 className="text-sm font-semibold">{t(s.label, { defaultValue: s.label })}</h3>
                  </div>
                  <p className="text-xs text-muted-foreground">{t(s.description, { defaultValue: '' })}</p>
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
