import { useState, useCallback, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  ShieldCheck, Eye, EyeOff, Lock, Mail, AlertCircle,
  CheckCircle2, Server, FileText, Activity, Shield,
} from 'lucide-react';
import { cn } from '../../../lib/cn';
import { useAuth } from '../../../contexts/AuthContext';
import { Button } from '../../../components/Button';
import { identityApi } from '../api';
import { resolveApiError } from '../../../utils/apiErrors';

/**
 * Página de login — Auth Shell enterprise (DESIGN-SYSTEM.md §4.2, DESIGN.md §9.1)
 *
 * Split-layout: hero esquerdo (55%) + auth card direito (45%).
 * Fundo navy profundo com halos radiais sutis.
 * Hero: headline grande com palavra mint de ênfase, chips de capacidade, trust signals.
 * Auth card: 420-460px, padding 40px, CTA gradient, SSO proeminente.
 */
export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [ssoLoading, setSsoLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const passwordRef = useRef<HTMLInputElement>(null);

  const clearSensitiveState = useCallback(() => {
    setPassword('');
    setShowPassword(false);
  }, []);

  const handleSsoLogin = async () => {
    setError(null);
    setSsoLoading(true);
    try {
      const returnTo = searchParams.get('returnTo') ?? undefined;
      const { authorizationUrl } = await identityApi.startOidcLogin('default', returnTo);
      window.location.href = authorizationUrl;
    } catch {
      setError(t('auth.ssoError'));
      setSsoLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const result = await login(email, password);
      clearSensitiveState();
      if (result === 'select-tenant') {
        navigate('/select-tenant');
      } else {
        navigate('/');
      }
    } catch (err) {
      setError(resolveApiError(err));
    } finally {
      setLoading(false);
    }
  };

  const platformCapabilities = [
    { icon: <Server size={16} />, labelKey: 'auth.capServiceGovernance' },
    { icon: <FileText size={16} />, labelKey: 'auth.capContractGovernance' },
    { icon: <Activity size={16} />, labelKey: 'auth.capChangeIntelligence' },
    { icon: <Shield size={16} />, labelKey: 'auth.capComplianceAudit' },
  ];

  return (
    <div className="min-h-screen bg-canvas flex relative overflow-hidden">
      {/* ── Background halos — profundidade navy com brilho sutil ──────────── */}
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute top-[-20%] left-[-10%] w-[60%] h-[60%] rounded-full bg-cyan/[0.04] blur-[120px]" />
        <div className="absolute bottom-[-20%] right-[-10%] w-[50%] h-[50%] rounded-full bg-mint/[0.03] blur-[120px]" />
      </div>

      {/* ── Left panel: Hero & Branding ──────────────────────────────────────── */}
      <div className="hidden lg:flex lg:w-[55%] xl:w-[55%] flex-col justify-between p-12 xl:p-16 relative">
        <div className="relative z-10">
          {/* Logo */}
          <div className="flex items-center gap-3 mb-20">
            <div className="w-11 h-11 rounded-lg bg-accent/12 flex items-center justify-center shadow-glow-sm">
              <span className="text-cyan font-bold text-lg">N</span>
            </div>
            <span className="font-semibold text-lg text-heading tracking-tight">NexTraceOne</span>
          </div>

          {/* Headline — display-01 scale, palavra de ênfase em mint */}
          <h1 className="text-4xl xl:text-5xl font-bold text-heading leading-[1.1] mb-5 max-w-xl">
            {t('auth.loginHeadline')}
          </h1>
          <p className="text-lg text-body leading-relaxed max-w-md mb-12">
            {t('auth.loginSubheadline')}
          </p>

          {/* Platform capabilities — chips com ícone */}
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

        {/* Trust signals — bottom */}
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

      {/* ── Right panel: Auth Card ───────────────────────────────────────────── */}
      <div className="flex-1 flex items-center justify-center p-6 sm:p-8 lg:p-12 relative z-10">
        <div className="w-full max-w-[440px] animate-fade-in">
          {/* Mobile-only logo */}
          <div className="lg:hidden text-center mb-10">
            <div className="inline-flex items-center justify-center w-12 h-12 rounded-lg bg-accent/12 mb-3 shadow-glow-sm">
              <span className="text-cyan font-bold text-lg">N</span>
            </div>
            <h1 className="text-xl font-bold text-heading">NexTraceOne</h1>
            <p className="text-sm text-muted mt-1">{t('auth.tagline')}</p>
          </div>

          {/* Auth card — DESIGN-SYSTEM.md: 420-460px, padding 40px, radius-lg */}
          <div className="bg-card rounded-lg shadow-elevated border border-edge p-10">
            {/* Accent stripe */}
            <div className="h-0.5 brand-gradient rounded-pill mb-8" />

            <h2 className="text-xl font-semibold text-heading mb-1">{t('auth.signIn')}</h2>
            <p className="text-sm text-muted mb-8">{t('auth.signInSubtitle')}</p>

            {/* Error banner */}
            {error && (
              <div
                role="alert"
                className="mb-6 rounded-md bg-critical/10 border border-critical/25 px-4 py-3 flex items-start gap-3 animate-fade-in"
              >
                <AlertCircle size={16} className="text-critical shrink-0 mt-0.5" />
                <p className="text-sm text-critical">{error}</p>
              </div>
            )}

            {/* SSO — primary authentication method */}
            <div className="mb-6">
              <Button
                type="button"
                variant="primary"
                size="lg"
                className="w-full"
                loading={ssoLoading}
                onClick={handleSsoLogin}
              >
                <ShieldCheck size={18} />
                {ssoLoading ? t('auth.ssoRedirecting') : t('auth.ssoSignIn')}
              </Button>
              <p className="text-xs text-muted text-center mt-2.5">{t('auth.ssoDescription')}</p>
            </div>

            {/* Divider */}
            <div className="relative flex items-center my-7">
              <div className="flex-1 border-t border-divider" />
              <span className="px-3 text-xs text-faded uppercase tracking-wider font-medium">
                {t('auth.orDivider')}
              </span>
              <div className="flex-1 border-t border-divider" />
            </div>

            {/* Credentials form — DESIGN-SYSTEM.md §4.4: input 56px, radius-lg, bg-input */}
            <form onSubmit={handleSubmit} className="space-y-5" noValidate>
              <div>
                <label className="block text-sm font-medium text-body mb-2" htmlFor="email">
                  {t('auth.email')}
                </label>
                <div className="relative">
                  <Mail size={16} className="absolute left-4 top-1/2 -translate-y-1/2 text-faded pointer-events-none" />
                  <input
                    id="email"
                    name="email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    placeholder={t('auth.emailPlaceholder')}
                    maxLength={254}
                    autoComplete="username"
                    spellCheck={false}
                    aria-describedby={error ? 'login-error' : undefined}
                    className={cn(
                      'w-full h-14 rounded-lg bg-input border border-edge',
                      'pl-11 pr-4 text-sm text-heading placeholder:text-faded',
                      'focus:outline-none focus:border-edge-focus focus:shadow-glow-cyan',
                      'transition-all duration-[var(--nto-motion-base)]',
                    )}
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-body mb-2" htmlFor="password">
                  {t('auth.password')}
                </label>
                <div className="relative">
                  <Lock size={16} className="absolute left-4 top-1/2 -translate-y-1/2 text-faded pointer-events-none" />
                  <input
                    ref={passwordRef}
                    id="password"
                    name="password"
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    placeholder={t('auth.passwordPlaceholder')}
                    maxLength={128}
                    autoComplete="current-password"
                    spellCheck={false}
                    aria-describedby={error ? 'login-error' : undefined}
                    className={cn(
                      'w-full h-14 rounded-lg bg-input border border-edge',
                      'pl-11 pr-11 text-sm text-heading placeholder:text-faded',
                      'focus:outline-none focus:border-edge-focus focus:shadow-glow-cyan',
                      'transition-all duration-[var(--nto-motion-base)]',
                    )}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword((v) => !v)}
                    className="absolute right-4 top-1/2 -translate-y-1/2 text-faded hover:text-muted transition-colors p-0.5"
                    tabIndex={-1}
                    aria-label={showPassword ? t('auth.hidePassword') : t('auth.showPassword')}
                  >
                    {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
              </div>

              <Button type="submit" loading={loading} className="w-full" size="lg">
                {t('auth.signInButton')}
              </Button>
            </form>

            {/* Help link */}
            <p className="text-center text-xs text-faded mt-6">
              {t('auth.needHelp')}{' '}
              <span className="text-cyan hover:text-cyan-hover cursor-pointer transition-colors">
                {t('auth.contactSupport')}
              </span>
            </p>
          </div>

          {/* Footer trust signals */}
          <div className="mt-6 flex items-center justify-center gap-3 text-xs text-faded flex-wrap">
            <span>{t('auth.selfHosted')}</span>
          </div>
        </div>
      </div>
    </div>
  );
}
