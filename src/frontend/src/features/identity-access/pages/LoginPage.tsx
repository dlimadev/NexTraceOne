import { useState, useCallback, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  ShieldCheck, Eye, EyeOff, Lock, Mail, AlertCircle,
  CheckCircle2, Server, FileText, Activity, Shield,
} from 'lucide-react';
import { useAuth } from '../../../contexts/AuthContext';
import { Button } from '../../../components/Button';
import { identityApi } from '../api';
import { resolveApiError } from '../../../utils/apiErrors';

/**
 * Página de login — enterprise split-layout, OIDC-first com fallback seguro.
 *
 * Layout: painel esquerdo com branding e capacidades da plataforma,
 * painel direito com bloco de autenticação premium.
 *
 * Segurança:
 * - Password nunca pré-preenchida nem armazenada em estado além do necessário.
 * - Input password com toggle seguro (type password/text).
 * - autoComplete correto para cada campo.
 * - Nenhum dado sensível logado ou exposto.
 * - Mensagens de erro genéricas (sem detalhes técnicos).
 * - Password limpa do estado após submissão bem-sucedida.
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

  /** Limpa a password do estado — chamado após login bem-sucedido. */
  const clearSensitiveState = useCallback(() => {
    setPassword('');
    setShowPassword(false);
  }, []);

  /** Inicia o fluxo OIDC/SSO redirecionando ao identity provider. */
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
    <div className="min-h-screen bg-canvas flex">
      {/* ── Left panel: Branding & Platform Context ──────────────────────────── */}
      <div className="hidden lg:flex lg:w-[45%] xl:w-[48%] flex-col justify-between p-10 xl:p-14 relative overflow-hidden">
        {/* Gradient background overlay */}
        <div className="absolute inset-0 bg-gradient-to-br from-accent/8 via-canvas to-brand-purple/6" />
        <div className="absolute top-0 left-0 right-0 h-1 brand-gradient" />

        <div className="relative z-10">
          {/* Logo */}
          <div className="flex items-center gap-3 mb-16">
            <div className="w-10 h-10 rounded-lg bg-accent/15 flex items-center justify-center shadow-glow-sm">
              <span className="text-accent font-bold text-lg">N</span>
            </div>
            <span className="font-semibold text-lg text-heading tracking-tight">NexTraceOne</span>
          </div>

          {/* Headline */}
          <h1 className="text-3xl xl:text-4xl font-bold text-heading leading-tight mb-4">
            {t('auth.loginHeadline')}
          </h1>
          <p className="text-base text-muted leading-relaxed max-w-md mb-10">
            {t('auth.loginSubheadline')}
          </p>

          {/* Platform capabilities */}
          <div className="space-y-4">
            {platformCapabilities.map((cap) => (
              <div key={cap.labelKey} className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-md bg-elevated border border-edge flex items-center justify-center text-accent shrink-0">
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

      {/* ── Right panel: Authentication ──────────────────────────────────────── */}
      <div className="flex-1 flex items-center justify-center p-6 sm:p-8 lg:p-12">
        <div className="w-full max-w-md animate-fade-in">
          {/* Mobile-only logo */}
          <div className="lg:hidden text-center mb-8">
            <div className="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-accent/15 mb-3">
              <span className="text-accent font-bold text-lg">N</span>
            </div>
            <h1 className="text-xl font-bold text-heading">NexTraceOne</h1>
            <p className="text-sm text-muted mt-1">{t('auth.tagline')}</p>
          </div>

          {/* Auth card */}
          <div className="bg-card rounded-xl shadow-lg border border-edge p-8">
            <div className="h-0.5 brand-gradient rounded-full mb-8" />

            <h2 className="text-xl font-semibold text-heading mb-1">{t('auth.signIn')}</h2>
            <p className="text-sm text-muted mb-8">{t('auth.signInSubtitle')}</p>

            {/* Error banner */}
            {error && (
              <div
                role="alert"
                className="mb-6 rounded-lg bg-critical/10 border border-critical/25 px-4 py-3 flex items-start gap-3 animate-fade-in"
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
            <div className="relative flex items-center my-6">
              <div className="flex-1 border-t border-edge" />
              <span className="px-3 text-xs text-faded uppercase tracking-wider font-medium">
                {t('auth.orDivider')}
              </span>
              <div className="flex-1 border-t border-edge" />
            </div>

            {/* Credentials form (fallback) */}
            <form onSubmit={handleSubmit} className="space-y-5" noValidate>
              <div>
                <label className="block text-sm font-medium text-body mb-1.5" htmlFor="email">
                  {t('auth.email')}
                </label>
                <div className="relative">
                  <Mail size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-faded pointer-events-none" />
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
                    className="w-full rounded-lg bg-canvas border border-edge pl-10 pr-3 py-2.5 text-sm text-heading placeholder:text-faded focus:outline-none focus:ring-2 focus:ring-accent/50 focus:border-accent transition-colors"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-body mb-1.5" htmlFor="password">
                  {t('auth.password')}
                </label>
                <div className="relative">
                  <Lock size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-faded pointer-events-none" />
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
                    className="w-full rounded-lg bg-canvas border border-edge pl-10 pr-10 py-2.5 text-sm text-heading placeholder:text-faded focus:outline-none focus:ring-2 focus:ring-accent/50 focus:border-accent transition-colors"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword((v) => !v)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-faded hover:text-muted transition-colors p-0.5"
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
              <span className="text-accent hover:text-accent-hover cursor-pointer">
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
