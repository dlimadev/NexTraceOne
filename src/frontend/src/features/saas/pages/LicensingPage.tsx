import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Award,
  Cpu,
  CheckCircle,
  RefreshCw,
  TrendingUp,
  Calendar,
  ShieldCheck,
} from 'lucide-react';
import { saasApi, type TenantPlan } from '../api/saasApi';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { Card, CardBody } from '../../../components/Card';

const PLAN_ORDER: TenantPlan[] = ['Trial', 'Starter', 'Professional', 'Enterprise'];

const PLAN_COLORS: Record<TenantPlan, string> = {
  Trial: 'bg-elevated text-muted border-edge',
  Starter: 'bg-accent/10 text-accent border-accent/20',
  Professional: 'bg-accent/10 text-accent border-accent/20',
  Enterprise: 'bg-warning/10 text-warning border-warning/20',
};

export function LicensingPage() {
  const { t } = useTranslation('saasLicensing');
  const qc = useQueryClient();
  const [upgradeTarget, setUpgradeTarget] = useState<TenantPlan | null>(null);

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['saas-license'],
    queryFn: saasApi.getLicense,
  });

  const upgradeMutation = useMutation({
    mutationFn: (plan: TenantPlan) =>
      saasApi.provisionLicense({ plan, includedHostUnits: data?.includedHostUnits ?? 0 }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['saas-license'] });
      setUpgradeTarget(null);
    },
  });

  const currentPlan = data?.plan ?? 'Starter';
  const currentPlanIndex = PLAN_ORDER.indexOf(currentPlan);

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Award size={20} />}
          actions={
            <Button
              variant="ghost"
              onClick={() => refetch()}
              disabled={isFetching}
              className="flex items-center gap-2"
              size="sm"
            >
              <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
              {t('refresh')}
            </Button>
          }
        />

        {isError && (
          <div className="bg-critical/10 border border-critical/20 text-critical rounded-lg p-4 text-sm">
            {t('loadError')}
          </div>
        )}

        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {[0, 1, 2].map((i) => (
              <div key={i} className="h-28 bg-elevated rounded-xl animate-pulse" />
            ))}
          </div>
        ) : data ? (
          <>
            {/* Summary Cards */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Card>
                <CardBody>
                  <div className="flex items-center gap-3 mb-3">
                    <Award size={20} className="text-accent" />
                    <span className="text-sm font-medium text-muted">{t('currentPlan')}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span
                      className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-semibold border ${PLAN_COLORS[data.plan]}`}
                    >
                      {data.plan}
                    </span>
                    <span
                      className={`text-xs px-2 py-0.5 rounded-full ${
                        data.status === 'Active' || data.status === 'Trial'
                          ? 'bg-success/10 text-success'
                          : 'bg-critical/10 text-critical'
                      }`}
                    >
                      {data.status}
                    </span>
                  </div>
                </CardBody>
              </Card>

              <Card>
                <CardBody>
                  <div className="flex items-center gap-3 mb-3">
                    <Cpu size={20} className="text-accent" />
                    <span className="text-sm font-medium text-muted">{t('hostUnits')}</span>
                  </div>
                  <div className="text-2xl font-bold text-heading">
                    {data.currentHostUnits.toFixed(1)}
                    <span className="text-sm font-normal text-muted ml-1">
                      / {data.includedHostUnits} {t('included')}
                    </span>
                  </div>
                  {data.overageHostUnits > 0 && (
                    <p className="text-xs text-warning mt-1">
                      +{data.overageHostUnits.toFixed(1)} {t('overage')}
                    </p>
                  )}
                </CardBody>
              </Card>

              <Card>
                <CardBody>
                  <div className="flex items-center gap-3 mb-3">
                    <Calendar size={20} className="text-muted" />
                    <span className="text-sm font-medium text-muted">{t('validity')}</span>
                  </div>
                  <div className="text-sm text-body">
                    <span className="font-medium">{t('from')}: </span>
                    {new Date(data.validFrom).toLocaleDateString()}
                  </div>
                  <div className="text-sm text-body">
                    <span className="font-medium">{t('until')}: </span>
                    {data.validUntil ? new Date(data.validUntil).toLocaleDateString() : t('noExpiry')}
                  </div>
                </CardBody>
              </Card>
            </div>

            {/* Capabilities */}
            <Card>
              <CardBody>
                <div className="flex items-center gap-2 mb-4">
                  <ShieldCheck size={18} className="text-accent" />
                  <h2 className="text-base font-semibold text-heading">{t('capabilities')}</h2>
                  <span className="ml-auto text-xs text-faded">
                    {data.capabilities.length} {t('capabilitiesEnabled')}
                  </span>
                </div>
                <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-2">
                  {data.capabilities.map((cap) => (
                    <div
                      key={cap}
                      className="flex items-center gap-2 text-sm text-body bg-elevated rounded-lg px-3 py-2"
                    >
                      <CheckCircle size={13} className="text-success shrink-0" />
                      <span className="truncate">{cap}</span>
                    </div>
                  ))}
                </div>
              </CardBody>
            </Card>

            {/* Upgrade Plans */}
            {currentPlanIndex < PLAN_ORDER.length - 1 && (
              <Card>
                <CardBody>
                  <div className="flex items-center gap-2 mb-4">
                    <TrendingUp size={18} className="text-accent" />
                    <h2 className="text-base font-semibold text-heading">{t('upgradePlan')}</h2>
                  </div>
                  <div className="flex flex-wrap gap-3">
                    {PLAN_ORDER.slice(currentPlanIndex + 1).map((plan) => (
                      <Button
                        key={plan}
                        variant="primary"
                        size="sm"
                        onClick={() => setUpgradeTarget(plan)}
                      >
                        {t('upgradeTo')} {plan}
                      </Button>
                    ))}
                  </div>
                </CardBody>
              </Card>
            )}
          </>
        ) : null}

        {/* Upgrade confirmation modal */}
        {upgradeTarget && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
            <div className="bg-card rounded-xl shadow-xl p-6 w-full max-w-sm mx-4 border border-edge">
              <h3 className="text-lg font-semibold text-heading mb-2">
                {t('confirmUpgrade')} {upgradeTarget}?
              </h3>
              <p className="text-sm text-muted mb-5">{t('confirmUpgradeDescription')}</p>
              <div className="flex gap-3 justify-end">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setUpgradeTarget(null)}
                >
                  {t('cancel')}
                </Button>
                <Button
                  variant="primary"
                  size="sm"
                  onClick={() => upgradeMutation.mutate(upgradeTarget)}
                  disabled={upgradeMutation.isPending}
                >
                  {upgradeMutation.isPending ? t('upgrading') : t('confirm')}
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </PageContainer>
  );
}
