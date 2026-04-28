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

const PLAN_ORDER: TenantPlan[] = ['Trial', 'Starter', 'Professional', 'Enterprise'];

const PLAN_COLORS: Record<TenantPlan, string> = {
  Trial: 'bg-slate-100 text-slate-700 border-slate-200',
  Starter: 'bg-blue-100 text-blue-700 border-blue-200',
  Professional: 'bg-violet-100 text-violet-700 border-violet-200',
  Enterprise: 'bg-amber-100 text-amber-700 border-amber-200',
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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="text-sm text-slate-500 mt-1">{t('subtitle')}</p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          className="flex items-center gap-2 text-sm text-slate-600 hover:text-slate-800 border border-slate-200 rounded-lg px-3 py-2 transition-colors"
        >
          <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
          {t('refresh')}
        </button>
      </div>

      {isError && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-4 text-sm">
          {t('loadError')}
        </div>
      )}

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-28 bg-slate-100 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : data ? (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-white border border-slate-200 rounded-xl p-5">
              <div className="flex items-center gap-3 mb-3">
                <Award size={20} className="text-violet-600" />
                <span className="text-sm font-medium text-slate-600">{t('currentPlan')}</span>
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
                      ? 'bg-green-100 text-green-700'
                      : 'bg-red-100 text-red-700'
                  }`}
                >
                  {data.status}
                </span>
              </div>
            </div>

            <div className="bg-white border border-slate-200 rounded-xl p-5">
              <div className="flex items-center gap-3 mb-3">
                <Cpu size={20} className="text-blue-600" />
                <span className="text-sm font-medium text-slate-600">{t('hostUnits')}</span>
              </div>
              <div className="text-2xl font-bold text-slate-900">
                {data.currentHostUnits.toFixed(1)}
                <span className="text-sm font-normal text-slate-500 ml-1">
                  / {data.includedHostUnits} {t('included')}
                </span>
              </div>
              {data.overageHostUnits > 0 && (
                <p className="text-xs text-amber-600 mt-1">
                  +{data.overageHostUnits.toFixed(1)} {t('overage')}
                </p>
              )}
            </div>

            <div className="bg-white border border-slate-200 rounded-xl p-5">
              <div className="flex items-center gap-3 mb-3">
                <Calendar size={20} className="text-slate-600" />
                <span className="text-sm font-medium text-slate-600">{t('validity')}</span>
              </div>
              <div className="text-sm text-slate-700">
                <span className="font-medium">{t('from')}: </span>
                {new Date(data.validFrom).toLocaleDateString()}
              </div>
              <div className="text-sm text-slate-700">
                <span className="font-medium">{t('until')}: </span>
                {data.validUntil ? new Date(data.validUntil).toLocaleDateString() : t('noExpiry')}
              </div>
            </div>
          </div>

          {/* Capabilities */}
          <div className="bg-white border border-slate-200 rounded-xl p-5">
            <div className="flex items-center gap-2 mb-4">
              <ShieldCheck size={18} className="text-violet-600" />
              <h2 className="text-base font-semibold text-slate-800">{t('capabilities')}</h2>
              <span className="ml-auto text-xs text-slate-400">
                {data.capabilities.length} {t('capabilitiesEnabled')}
              </span>
            </div>
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-2">
              {data.capabilities.map((cap) => (
                <div
                  key={cap}
                  className="flex items-center gap-2 text-sm text-slate-700 bg-slate-50 rounded-lg px-3 py-2"
                >
                  <CheckCircle size={13} className="text-green-500 shrink-0" />
                  <span className="truncate">{cap}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Upgrade Plans */}
          {currentPlanIndex < PLAN_ORDER.length - 1 && (
            <div className="bg-white border border-slate-200 rounded-xl p-5">
              <div className="flex items-center gap-2 mb-4">
                <TrendingUp size={18} className="text-blue-600" />
                <h2 className="text-base font-semibold text-slate-800">{t('upgradePlan')}</h2>
              </div>
              <div className="flex flex-wrap gap-3">
                {PLAN_ORDER.slice(currentPlanIndex + 1).map((plan) => (
                  <button
                    key={plan}
                    onClick={() => setUpgradeTarget(plan)}
                    className="px-4 py-2 rounded-lg border text-sm font-medium transition-colors bg-blue-600 text-white border-blue-600 hover:bg-blue-700"
                  >
                    {t('upgradeTo')} {plan}
                  </button>
                ))}
              </div>
            </div>
          )}
        </>
      ) : null}

      {/* Upgrade confirmation modal */}
      {upgradeTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl shadow-xl p-6 w-full max-w-sm mx-4">
            <h3 className="text-lg font-semibold text-slate-900 mb-2">
              {t('confirmUpgrade')} {upgradeTarget}?
            </h3>
            <p className="text-sm text-slate-500 mb-5">{t('confirmUpgradeDescription')}</p>
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => setUpgradeTarget(null)}
                className="px-4 py-2 text-sm text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50"
              >
                {t('cancel')}
              </button>
              <button
                onClick={() => upgradeMutation.mutate(upgradeTarget)}
                disabled={upgradeMutation.isPending}
                className="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-60"
              >
                {upgradeMutation.isPending ? t('upgrading') : t('confirm')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
