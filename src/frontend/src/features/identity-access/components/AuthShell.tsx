import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Lock, CheckCircle2, Server, FileText, Activity, Shield,
} from 'lucide-react';
import { cn } from '../../../lib/cn';
import { NexTraceLogo } from '../../../components/NexTraceLogo';

interface AuthShellProps {
  children: ReactNode;
  /** Max-width of the auth card area. Default: max-w-[460px] */
  cardMaxWidth?: string;
}

/**
 * Auth Shell — layout reutilizável para todas as telas de autenticação.
 *
 * DESIGN-SYSTEM.md §4.2: split-layout 55/45, hero esquerdo, card direito.
 * Hero: headline, subtítulo, chips de capacidade, trust signals.
 * Auth card area: responsivo, centralizado.
 *
 * Breakpoints:
 * - lg+: split-layout com hero + card
 * - <lg: card centralizado com logo mobile
 */
export function AuthShell({ children, cardMaxWidth = 'max-w-[460px]' }: AuthShellProps) {
  const { t } = useTranslation();

  const platformCapabilities = [
    {
      icon: <Server size={16} />,
      labelKey: 'auth.capServiceGovernance',
      color: 'text-blue',
      bg: 'bg-blue-muted border-blue/20',
    },
    {
      icon: <FileText size={16} />,
      labelKey: 'auth.capContractGovernance',
      color: 'text-cyan',
      bg: 'bg-cyan/10 border-cyan/20',
    },
    {
      icon: <Activity size={16} />,
      labelKey: 'auth.capChangeIntelligence',
      color: 'text-warning',
      bg: 'bg-warning/10 border-warning/20',
    },
    {
      icon: <Shield size={16} />,
      labelKey: 'auth.capComplianceAudit',
      color: 'text-mint',
      bg: 'bg-success/10 border-success/20',
    },
  ];

  const trustSignals = [
    t('auth.trustTrusted', 'Trusted'),
    t('auth.trustAuditable', 'Auditable'),
    t('auth.trustScalable', 'Scalable'),
    t('auth.trustEnterpriseReady', 'Enterprise-ready'),
  ];

  return (
    <div className="min-h-screen bg-canvas flex relative overflow-hidden">
      {/* Background: hero radial halos */}
      <div className="absolute inset-0 pointer-events-none" aria-hidden="true">
        <div
          className="absolute top-[-15%] left-[-5%] w-[55%] h-[55%] rounded-full blur-[140px]"
          style={{ background: 'radial-gradient(circle, rgba(27,127,232,0.10) 0%, transparent 70%)' }}
        />
        <div
          className="absolute bottom-[-20%] right-[-10%] w-[45%] h-[50%] rounded-full blur-[120px]"
          style={{ background: 'radial-gradient(circle, rgba(18,196,232,0.07) 0%, transparent 70%)' }}
        />
        <div
          className="absolute top-[40%] left-[25%] w-[30%] h-[30%] rounded-full blur-[100px]"
          style={{ background: 'radial-gradient(circle, rgba(24,232,184,0.04) 0%, transparent 70%)' }}
        />
      </div>

      {/* Left panel: Hero & Branding — hidden below lg */}
      <div
        className="hidden lg:flex lg:w-[55%] xl:w-[55%] flex-col justify-between p-12 xl:p-16 relative border-r border-edge"
        style={{ background: 'var(--t-sidebar-gradient)' }}
      >
        {/* Subtle grid overlay */}
        <div
          className="absolute inset-0 pointer-events-none opacity-[0.03]"
          style={{
            backgroundImage: 'linear-gradient(rgba(129,170,214,1) 1px, transparent 1px), linear-gradient(90deg, rgba(129,170,214,1) 1px, transparent 1px)',
            backgroundSize: '48px 48px',
          }}
          aria-hidden="true"
        />

        <div className="relative z-10">
          {/* Logo */}
          <div className="mb-16">
            <NexTraceLogo size={44} variant="full" />
          </div>

          {/* Headline */}
          <h1 className="text-4xl xl:text-[2.75rem] font-bold text-heading leading-[1.1] mb-5 max-w-lg">
            {t('auth.loginHeadline')}
          </h1>
          <p className="text-base text-body leading-relaxed max-w-md mb-12 opacity-80">
            {t('auth.loginSubheadline')}
          </p>

          {/* Platform capabilities list */}
          <div className="space-y-3">
            {platformCapabilities.map((cap) => (
              <div key={cap.labelKey} className="flex items-center gap-3.5">
                <div className={cn('w-8 h-8 rounded-md border flex items-center justify-center shrink-0', cap.bg, cap.color)}>
                  {cap.icon}
                </div>
                <span className="text-sm text-body">{t(cap.labelKey)}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Footer: trust + encryption signals */}
        <div className="relative z-10">
          {/* Trust badges row */}
          <div className="flex items-center gap-2 flex-wrap mb-4">
            {trustSignals.map((signal, i) => (
              <span key={signal} className="text-xs font-medium text-faded">
                {signal}
                {i < trustSignals.length - 1 && (
                  <span className="ml-2 text-faded/40">·</span>
                )}
              </span>
            ))}
          </div>
          {/* Security signals */}
          <div className="flex items-center gap-5 text-xs text-faded/70">
            <span className="flex items-center gap-1.5">
              <Lock size={11} />
              {t('auth.trustEncrypted')}
            </span>
            <span className="flex items-center gap-1.5">
              <CheckCircle2 size={11} />
              {t('auth.trustCompliant')}
            </span>
          </div>
        </div>
      </div>

      {/* Right panel: Auth Card */}
      <div className="flex-1 flex items-center justify-center p-6 sm:p-8 lg:p-12 relative z-10">
        <div className={cn('w-full animate-fade-in', cardMaxWidth)}>
          {/* Mobile-only logo */}
          <div className="lg:hidden text-center mb-10">
            <div className="flex justify-center mb-4">
              <NexTraceLogo size={48} variant="icon" />
            </div>
            <div className="flex justify-center">
              <NexTraceLogo size={36} variant="compact" />
            </div>
            <p className="text-sm text-muted mt-2">{t('auth.tagline')}</p>
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}
