import { useTranslation } from 'react-i18next';
import { Badge } from '../../../components/Badge';
import { cn } from '../../../lib/cn';
import type { ServiceIdentityValues } from './onboardValidation';

/** Cartão de identidade ao vivo (rail esquerdo do onboarding). Novo serviço é sempre Planning. */
export function OnboardIdentityPreview({ values }: { values: ServiceIdentityValues }) {
  const { t } = useTranslation();
  const initial = values.name.trim().charAt(0).toUpperCase() || '?';

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          <div className={cn(
            'flex items-center justify-center w-11 h-11 rounded-xl font-bold text-lg shrink-0',
            values.name ? 'bg-accent text-on-accent' : 'bg-accent/20 text-accent',
          )}>
            {initial}
          </div>
          <div className="min-w-0">
            <p className={cn('font-mono text-sm font-semibold truncate', values.name ? 'text-heading' : 'text-muted')}>
              {values.name || t('onboard.identity.name')}
            </p>
            <p className="text-xs text-muted truncate mt-0.5">{values.domain || '—'}</p>
          </div>
          <Badge variant="warning" size="sm" className="shrink-0 ml-auto">Planning</Badge>
        </div>
        <div className="flex flex-wrap gap-1.5 mt-3">
          {values.serviceType && <Badge variant="primary" size="sm">{values.serviceType}</Badge>}
          {values.criticality && <Badge variant="default" size="sm">{values.criticality}</Badge>}
          {values.exposureType && <Badge variant="default" size="sm">{values.exposureType}</Badge>}
        </div>
      </div>
      <div className="px-4 py-2 divide-y divide-edge/60">
        <Row label={t('onboard.identity.team')} value={values.teamName || '—'} />
        <Row label={t('onboard.identity.technicalOwner')} value={values.technicalOwner || '—'} />
      </div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-2 text-xs">
      <span className="text-muted">{label}</span>
      <span className="text-heading font-medium truncate ml-2">{value}</span>
    </div>
  );
}
