import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import {
  Building2,
  ChevronRight,
  ChevronLeft,
  CheckCircle,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import { saasApi, type TenantPlan, type ProvisionTenantResponse } from '../api/saasApi';

type Step = 1 | 2 | 3;

interface FormData {
  name: string;
  slug: string;
  plan: TenantPlan;
  includedHostUnits: number;
  legalName: string;
  taxId: string;
}

const PLAN_DESCRIPTIONS: Record<TenantPlan, { label: string; color: string; features: string[] }> = {
  Trial: {
    label: 'Trial',
    color: 'border-slate-300',
    features: ['14-day trial', 'All Enterprise features', 'No credit card required'],
  },
  Starter: {
    label: 'Starter',
    color: 'border-blue-400',
    features: ['Core monitoring', 'Up to 10 host units', 'Email support'],
  },
  Professional: {
    label: 'Professional',
    color: 'border-violet-400',
    features: ['Advanced analytics', 'Up to 50 host units', 'AI governance', 'Priority support'],
  },
  Enterprise: {
    label: 'Enterprise',
    color: 'border-amber-400',
    features: ['Unlimited host units', 'Full AI suite', 'SSO / SAML', 'Dedicated support'],
  },
};

function StepIndicator({ current, total }: { current: Step; total: number }) {
  return (
    <div className="flex items-center gap-2 mb-6">
      {Array.from({ length: total }, (_, i) => i + 1).map((step) => (
        <div key={step} className="flex items-center gap-2">
          <div
            className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-semibold transition-colors ${
              step < current
                ? 'bg-green-500 text-white'
                : step === current
                ? 'bg-blue-600 text-white'
                : 'bg-slate-100 text-slate-400'
            }`}
          >
            {step < current ? <CheckCircle size={14} /> : step}
          </div>
          {step < total && <div className={`h-0.5 w-8 ${step < current ? 'bg-green-400' : 'bg-slate-200'}`} />}
        </div>
      ))}
    </div>
  );
}

function slugify(value: string): string {
  return value
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, '')
    .replace(/^-+|-+$/g, '');
}

export function TenantProvisioningPage() {
  const { t } = useTranslation('tenantProvisioning');
  const navigate = useNavigate();
  const [step, setStep] = useState<Step>(1);
  const [form, setForm] = useState<FormData>({
    name: '',
    slug: '',
    plan: 'Starter',
    includedHostUnits: 5,
    legalName: '',
    taxId: '',
  });
  const [result, setResult] = useState<ProvisionTenantResponse | null>(null);

  const mutation = useMutation({
    mutationFn: () =>
      saasApi.provisionTenant({
        name: form.name,
        slug: form.slug,
        plan: form.plan,
        includedHostUnits: form.includedHostUnits,
        legalName: form.legalName || undefined,
        taxId: form.taxId || undefined,
      }),
    onSuccess: (data) => {
      setResult(data);
      setStep(3);
    },
  });

  function handleNameChange(value: string) {
    setForm((f) => ({ ...f, name: value, slug: slugify(value) }));
  }

  const canProceedStep1 = form.name.trim().length >= 2 && /^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$/.test(form.slug);

  return (
    <div className="p-6">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <div className="flex items-center gap-3 mb-6">
          <Building2 size={24} className="text-blue-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>

        <div className="bg-white border border-slate-200 rounded-xl p-6">
          <StepIndicator current={step} total={3} />

          {/* Step 1: Identity */}
          {step === 1 && (
            <div className="space-y-4">
              <h2 className="text-base font-semibold text-slate-800">{t('step1.title')}</h2>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  {t('step1.name')} <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => handleNameChange(e.target.value)}
                  placeholder={t('step1.namePlaceholder')}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  {t('step1.slug')} <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={form.slug}
                  onChange={(e) => setForm((f) => ({ ...f, slug: slugify(e.target.value) }))}
                  placeholder="my-tenant"
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <p className="text-xs text-slate-400 mt-1">{t('step1.slugHint')}</p>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  {t('step1.legalName')}
                </label>
                <input
                  type="text"
                  value={form.legalName}
                  onChange={(e) => setForm((f) => ({ ...f, legalName: e.target.value }))}
                  placeholder={t('step1.legalNamePlaceholder')}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  {t('step1.taxId')}
                </label>
                <input
                  type="text"
                  value={form.taxId}
                  onChange={(e) => setForm((f) => ({ ...f, taxId: e.target.value }))}
                  placeholder="123-456-789"
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="flex justify-end pt-2">
                <button
                  onClick={() => setStep(2)}
                  disabled={!canProceedStep1}
                  className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {t('next')} <ChevronRight size={16} />
                </button>
              </div>
            </div>
          )}

          {/* Step 2: Plan & Host Units */}
          {step === 2 && (
            <div className="space-y-4">
              <h2 className="text-base font-semibold text-slate-800">{t('step2.title')}</h2>

              <div className="grid grid-cols-2 gap-3">
                {(Object.keys(PLAN_DESCRIPTIONS) as TenantPlan[]).map((plan) => {
                  const desc = PLAN_DESCRIPTIONS[plan];
                  return (
                    <button
                      key={plan}
                      onClick={() => setForm((f) => ({ ...f, plan }))}
                      className={`text-left p-4 rounded-xl border-2 transition-all ${
                        form.plan === plan
                          ? `${desc.color} bg-blue-50`
                          : 'border-slate-200 hover:border-slate-300'
                      }`}
                    >
                      <div className="font-semibold text-slate-800 mb-2">{desc.label}</div>
                      <ul className="space-y-1">
                        {desc.features.map((f) => (
                          <li key={f} className="text-xs text-slate-500 flex items-center gap-1.5">
                            <CheckCircle size={10} className="text-green-500 shrink-0" />
                            {f}
                          </li>
                        ))}
                      </ul>
                    </button>
                  );
                })}
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  {t('step2.includedHostUnits')}
                </label>
                <input
                  type="number"
                  min={0}
                  max={1000}
                  value={form.includedHostUnits}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, includedHostUnits: Math.max(0, parseInt(e.target.value) || 0) }))
                  }
                  className="w-32 border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <p className="text-xs text-slate-400 mt-1">{t('step2.hostUnitsHint')}</p>
              </div>

              <div className="flex justify-between pt-2">
                <button
                  onClick={() => setStep(1)}
                  className="flex items-center gap-2 px-4 py-2 text-sm text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50"
                >
                  <ChevronLeft size={16} /> {t('back')}
                </button>
                <button
                  onClick={() => mutation.mutate()}
                  disabled={mutation.isPending}
                  className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-60"
                >
                  {mutation.isPending ? (
                    <>
                      <Loader2 size={14} className="animate-spin" /> {t('provisioning')}
                    </>
                  ) : (
                    <>
                      {t('provision')} <ChevronRight size={16} />
                    </>
                  )}
                </button>
              </div>

              {mutation.isError && (
                <div className="flex items-center gap-2 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg p-3 mt-2">
                  <AlertCircle size={14} />
                  {t('provisionError')}
                </div>
              )}
            </div>
          )}

          {/* Step 3: Success */}
          {step === 3 && result && (
            <div className="text-center py-6 space-y-4">
              <CheckCircle size={48} className="mx-auto text-green-500" />
              <h2 className="text-lg font-semibold text-slate-900">{t('step3.title')}</h2>
              <div className="bg-slate-50 rounded-xl p-4 text-left text-sm space-y-2">
                <div>
                  <span className="text-slate-500">{t('step3.tenantId')}: </span>
                  <span className="font-mono text-xs text-slate-700">{result.tenantId}</span>
                </div>
                <div>
                  <span className="text-slate-500">{t('step3.name')}: </span>
                  <span className="font-medium text-slate-800">{result.name}</span>
                </div>
                <div>
                  <span className="text-slate-500">{t('step3.plan')}: </span>
                  <span className="font-medium text-slate-800">{result.plan}</span>
                </div>
                <div>
                  <span className="text-slate-500">{t('step3.license')}: </span>
                  <span className={result.licenseProvisioned ? 'text-green-600 font-medium' : 'text-amber-600'}>
                    {result.licenseProvisioned ? t('step3.licenseOk') : t('step3.licensePartial')}
                  </span>
                </div>
              </div>
              <div className="flex gap-3 justify-center pt-2">
                <button
                  onClick={() => navigate('/admin/tenants')}
                  className="px-4 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50"
                >
                  {t('step3.goToTenants')}
                </button>
                <button
                  onClick={() => {
                    setStep(1);
                    setForm({ name: '', slug: '', plan: 'Starter', includedHostUnits: 5, legalName: '', taxId: '' });
                    setResult(null);
                  }}
                  className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                >
                  {t('step3.provisionAnother')}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
