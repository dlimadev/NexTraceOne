import type React from 'react';
import { useTranslation } from 'react-i18next';
import type { ServiceIdentityValues, ServiceInterfaceValues } from './onboardValidation';

interface OnboardReviewStepProps {
  identity: ServiceIdentityValues;
  interfaceValues: ServiceInterfaceValues | null;
  contractSummary: { title: string; type: string | null } | null;
}

/** Resumo honest-null do que foi/será criado. Passos saltados mostram "Skipped". */
export function OnboardReviewStep({ identity, interfaceValues, contractSummary }: OnboardReviewStepProps) {
  const { t } = useTranslation();
  const skipped = t('onboard.review.skipped', 'Skipped');

  return (
    <div className="space-y-4">
      <h2 className="text-base font-semibold text-heading">{t('onboard.review.heading', 'Review & create')}</h2>

      <Section title={t('onboard.review.service', 'Service')}>
        <Row label={t('onboard.identity.name')} value={identity.name || '—'} />
        <Row label={t('onboard.identity.domain')} value={identity.domain || '—'} />
        <Row label={t('onboard.identity.team')} value={identity.teamName || '—'} />
        <Row label={t('onboard.identity.serviceType')} value={identity.serviceType ? t(`catalog.badges.type.${identity.serviceType}`, identity.serviceType) : '—'} />
      </Section>

      <Section title={t('onboard.review.interface', 'Interface')}>
        {interfaceValues
          ? <Row label={t('onboard.identity.name')} value={interfaceValues.name || '—'} />
          : <p className="text-sm text-muted py-1">{skipped}</p>}
      </Section>

      <Section title={t('onboard.review.contract', 'Contract')}>
        {contractSummary
          ? <Row label={t('onboard.identity.name')} value={contractSummary.title || contractSummary.type || '—'} />
          : <p className="text-sm text-muted py-1">{skipped}</p>}
      </Section>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-edge bg-deep p-4">
      <p className="text-xs font-semibold uppercase tracking-wider text-muted mb-2">{title}</p>
      <dl className="divide-y divide-edge/60">{children}</dl>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-1.5 text-sm">
      <dt className="text-muted">{label}</dt>
      <dd className="text-heading font-medium truncate ml-2">{value}</dd>
    </div>
  );
}
