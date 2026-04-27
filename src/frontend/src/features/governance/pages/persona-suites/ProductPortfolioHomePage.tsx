import { useTranslation } from 'react-i18next';
import { BarChart3, TrendingUp, DollarSign, CheckCircle2, Package } from 'lucide-react';
import { Link } from 'react-router-dom';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';

const SECTIONS = [
  { key: 'capabilities', label: 'product.businessCapabilities', icon: <Package size={16} />, to: '/governance/domains', color: 'text-accent' },
  { key: 'delivery', label: 'product.deliveryVelocity', icon: <TrendingUp size={16} />, to: '/governance/dora-metrics', color: 'text-success' },
  { key: 'slo-compliance', label: 'product.sloCompliance', icon: <CheckCircle2 size={16} />, to: '/operations/slos', color: 'text-info' },
  { key: 'cost', label: 'product.costPerProduct', icon: <DollarSign size={16} />, to: '/governance/finops', color: 'text-warning' },
  { key: 'risk', label: 'product.aggregatedRisk', icon: <BarChart3 size={16} />, to: '/governance/risk', color: 'text-destructive' },
];

export function ProductPortfolioHomePage() {
  const { t } = useTranslation();

  return (
    <PageContainer>
      <PageHeader
        title={t('personaSuite.product.title')}
        subtitle={t('personaSuite.product.subtitle')}
        actions={<Badge variant="secondary">{t('personaSuite.product.role')}</Badge>}
      />
      <PageSection>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {SECTIONS.map((s) => (
            <Link key={s.key} to={s.to}>
              <Card className="hover:border-accent/60 transition-colors h-full">
                <CardBody className="p-4 flex items-center gap-3">
                  <span className={s.color}>{s.icon}</span>
                  <span className="text-sm font-medium">{t(s.label, { defaultValue: s.label })}</span>
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
