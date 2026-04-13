import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Lock, CheckCircle2, Server, FileText, Activity, Shield,
} from 'lucide-react';
import { cn } from '../../../lib/cn';
import { NexTraceLogo } from '../../../components/NexTraceLogo';
import { ThemeToggle } from '../../../shared/ui';

interface AuthShellProps {
  children: ReactNode;
  /** Max-width of the auth card area. Default: max-w-[460px] */
  cardMaxWidth?: string;
}

/**
 * Auth Shell — layout reutilizável para todas as telas de autenticação.
 *
 * Inspirado no NexLink template login-cover: split-layout 50/50.
 * Esquerda: ilustração + headline + capacidades + trust signals.
 * Direita: card de autenticação centrado.
 * Pill theme toggle no canto superior direito.
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
    <div className="min-h-screen bg-canvas flex relative overflow-x-hidden overflow-y-auto">
      {/* Background radial halos */}
      <div className="absolute inset-0 pointer-events-none" aria-hidden="true">
        <div
          className="absolute top-[-15%] left-[-5%] w-[55%] h-[55%] rounded-full blur-[140px] bg-[radial-gradient(circle,rgba(27,127,232,0.10)_0%,transparent_70%)]"
        />
        <div
          className="absolute bottom-[-20%] right-[-10%] w-[45%] h-[50%] rounded-full blur-[120px] bg-[radial-gradient(circle,rgba(18,196,232,0.07)_0%,transparent_70%)]"
        />
      </div>

      {/* Theme toggle — top-right corner */}
      <div className="fixed top-5 right-5 z-50">
        <ThemeToggle />
      </div>

      {/* Left panel: Illustration + Branding — hidden below lg */}
      <div
        className="hidden lg:flex lg:w-1/2 flex-col justify-center p-12 xl:p-16 relative bg-[var(--t-sidebar-gradient)]"
      >
        {/* Subtle grid overlay */}
        <div
          className="absolute inset-0 pointer-events-none opacity-[0.03] [background-image:linear-gradient(rgba(129,170,214,1)_1px,transparent_1px),linear-gradient(90deg,rgba(129,170,214,1)_1px,transparent_1px)] [background-size:48px_48px]"
          aria-hidden="true"
        />

        {/* Brand logo + content, top-left aligned */}
        <div className="relative z-10 flex-1 flex flex-col items-start justify-start text-left pt-4">
          {/* Full NexTraceOne logo (globe + wordmark) */}
          <div className="mb-10">
            <img
              src="/brand/logo.svg"
              alt="NexTraceOne"
              className="h-16 xl:h-20 w-auto"
            />
          </div>

          {/* Headline */}
          <h1 className="text-2xl xl:text-3xl font-bold text-heading leading-tight mb-4 max-w-lg">
            {t('auth.loginHeadline')}
          </h1>
          <p className="text-sm text-body leading-relaxed max-w-md mb-10 opacity-80">
            {t('auth.loginSubheadline')}
          </p>

          {/* Platform capabilities list */}
          <div className="space-y-2.5 mb-10">
            {platformCapabilities.map((cap) => (
              <div key={cap.labelKey} className="flex items-center gap-3">
                <div className={cn('w-7 h-7 rounded-md border flex items-center justify-center shrink-0', cap.bg, cap.color)}>
                  {cap.icon}
                </div>
                <span className="text-sm text-body">{t(cap.labelKey)}</span>
              </div>
            ))}
          </div>

          {/* Trust + security footer */}
          <div className="flex items-center gap-2 flex-wrap mb-3">
            {trustSignals.map((signal, i) => (
              <span key={signal} className="text-xs font-medium text-faded">
                {signal}
                {i < trustSignals.length - 1 && (
                  <span className="ml-2 text-faded/40">·</span>
                )}
              </span>
            ))}
          </div>
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
