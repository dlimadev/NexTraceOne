import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Lock, CheckCircle2, Server, FileText, Activity, Shield,
} from 'lucide-react';
import { cn } from '../../../lib/cn';

interface AuthShellProps {
  children: ReactNode;
  /** Max-width of the auth card area. Default: max-w-[440px] */
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
export function AuthShell({ children, cardMaxWidth = 'max-w-[440px]' }: AuthShellProps) {
  const { t } = useTranslation();

  const platformCapabilities = [
    { icon: <Server size={16} />, labelKey: 'auth.capServiceGovernance' },
    { icon: <FileText size={16} />, labelKey: 'auth.capContractGovernance' },
    { icon: <Activity size={16} />, labelKey: 'auth.capChangeIntelligence' },
    { icon: <Shield size={16} />, labelKey: 'auth.capComplianceAudit' },
  ];

  return (
    <div className="min-h-screen bg-canvas flex relative overflow-hidden">
      {/* Background halos */}
      <div className="absolute inset-0 pointer-events-none" aria-hidden="true">
        <div className="absolute top-[-20%] left-[-10%] w-[60%] h-[60%] rounded-full bg-cyan/[0.04] blur-[120px]" />
        <div className="absolute bottom-[-20%] right-[-10%] w-[50%] h-[50%] rounded-full bg-mint/[0.03] blur-[120px]" />
      </div>

      {/* Left panel: Hero & Branding — hidden below lg */}
      <div className="hidden lg:flex lg:w-[55%] xl:w-[55%] flex-col justify-between p-12 xl:p-16 relative">
        <div className="relative z-10">
          <AuthLogo />
          <h1 className="text-4xl xl:text-5xl font-bold text-heading leading-[1.1] mb-5 max-w-xl">
            {t('auth.loginHeadline')}
          </h1>
          <p className="text-lg text-body leading-relaxed max-w-md mb-12">
            {t('auth.loginSubheadline')}
          </p>
          <div className="space-y-3.5">
            {platformCapabilities.map((cap) => (
              <div key={cap.labelKey} className="flex items-center gap-3.5">
                <div className="w-9 h-9 rounded-md bg-elevated border border-edge flex items-center justify-center text-cyan shrink-0">
                  {cap.icon}
                </div>
                <span className="text-sm text-body">{t(cap.labelKey)}</span>
              </div>
            ))}
          </div>
        </div>
        <div className="relative z-10 flex items-center gap-6 text-xs text-faded">
          <span className="flex items-center gap-1.5">
            <Lock size={12} />
            {t('auth.trustEncrypted')}
          </span>
          <span className="flex items-center gap-1.5">
            <CheckCircle2 size={12} />
            {t('auth.trustCompliant')}
          </span>
        </div>
      </div>

      {/* Right panel: Auth Card */}
      <div className="flex-1 flex items-center justify-center p-6 sm:p-8 lg:p-12 relative z-10">
        <div className={cn('w-full animate-fade-in', cardMaxWidth)}>
          {/* Mobile-only logo */}
          <div className="lg:hidden text-center mb-10">
            <AuthLogo centered />
            <h1 className="text-xl font-bold text-heading">NexTraceOne</h1>
            <p className="text-sm text-muted mt-1">{t('auth.tagline')}</p>
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}

function AuthLogo({ centered }: { centered?: boolean }) {
  return (
    <div className={cn('flex items-center gap-3', centered ? 'justify-center mb-3' : 'mb-20')}>
      <div className="w-11 h-11 rounded-lg bg-accent/12 flex items-center justify-center shadow-glow-sm">
        <span className="text-cyan font-bold text-lg">N</span>
      </div>
      {!centered && (
        <span className="font-semibold text-lg text-heading tracking-tight">NexTraceOne</span>
      )}
    </div>
  );
}
