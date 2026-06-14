import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Lock, CheckCircle2, Check } from 'lucide-react';
import { cn } from '../../../lib/cn';
import { NexTraceLogo } from '../../../components/NexTraceLogo';
import { ThemeToggle } from '../../../shared/ui';

interface AuthShellProps {
  children: ReactNode;
  /** Max-width of the auth card area. Default: max-w-[440px] */
  cardMaxWidth?: string;
}

/**
 * Auth Shell — layout reutilizável para todas as telas de autenticação.
 *
 * Estética Betterstack: split-layout 50/50, minimalista e elegante.
 * Esquerda: painel flat com logo, headline, lista enxuta de capacidades e trust.
 * Direita: card de autenticação centrado.
 * Um único halo azul sutil no fundo; pill theme toggle no canto superior direito.
 */
export function AuthShell({ children, cardMaxWidth = 'max-w-[440px]' }: AuthShellProps) {
  const { t } = useTranslation();

  const capabilities = [
    'auth.capServiceGovernance',
    'auth.capContractGovernance',
    'auth.capChangeIntelligence',
    'auth.capComplianceAudit',
  ];

  const trustSignals = [
    t('auth.trustTrusted', 'Trusted'),
    t('auth.trustAuditable', 'Auditable'),
    t('auth.trustScalable', 'Scalable'),
    t('auth.trustEnterpriseReady', 'Enterprise-ready'),
  ];

  return (
    <div className="min-h-screen bg-canvas flex relative overflow-x-hidden overflow-y-auto">
      {/* Halo azul único e sutil — profundidade discreta no fundo */}
      <div
        className="absolute top-[-20%] left-[-10%] w-[60%] h-[60%] rounded-full blur-[160px] pointer-events-none bg-[radial-gradient(circle,rgba(59,130,246,0.08)_0%,transparent_70%)]"
        aria-hidden="true"
      />

      {/* Theme toggle — top-right corner */}
      <div className="fixed top-5 right-5 z-50">
        <ThemeToggle />
      </div>

      {/* Left panel: branding minimalista — hidden below lg */}
      <div className="hidden lg:flex lg:w-1/2 flex-col justify-between p-12 xl:p-16 relative bg-deep border-r border-edge">
        {/* Grid overlay ultra-sutil, neutro */}
        <div
          className="absolute inset-0 pointer-events-none opacity-[0.025] [background-image:linear-gradient(rgba(255,255,255,1)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,1)_1px,transparent_1px)] [background-size:56px_56px]"
          aria-hidden="true"
        />

        {/* Logo no topo */}
        <div className="relative z-10">
          <img src="/brand/logo.svg" alt="NexTraceOne" className="h-12 xl:h-14 w-auto" />
        </div>

        {/* Headline + capacidades — bloco central */}
        <div className="relative z-10 max-w-md">
          <h1 className="text-3xl xl:text-4xl font-bold text-heading leading-[1.15] tracking-tight mb-4">
            {t('auth.loginHeadline')}
          </h1>
          <p className="text-base text-muted leading-relaxed mb-10">
            {t('auth.loginSubheadline')}
          </p>

          <ul className="space-y-3.5">
            {capabilities.map((labelKey) => (
              <li key={labelKey} className="flex items-center gap-3">
                <span className="flex h-5 w-5 items-center justify-center rounded-full bg-accent-muted shrink-0">
                  <Check size={12} className="text-accent" />
                </span>
                <span className="text-sm text-body">{t(labelKey)}</span>
              </li>
            ))}
          </ul>
        </div>

        {/* Trust + segurança — rodapé */}
        <div className="relative z-10">
          <div className="flex items-center gap-2 flex-wrap mb-3">
            {trustSignals.map((signal, i) => (
              <span key={signal} className="text-xs font-medium text-faded">
                {signal}
                {i < trustSignals.length - 1 && <span className="ml-2 text-faded/40">·</span>}
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
