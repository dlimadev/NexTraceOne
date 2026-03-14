import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ShieldCheck } from 'lucide-react';
import { useAuth } from '../../../contexts/AuthContext';
import { Button } from '../../../components/Button';
import { identityApi } from '../api';
import { resolveApiError } from '../../../utils/apiErrors';

/**
 * Página de login — OIDC-first com fallback para email e senha.
 *
 * A experiência prioriza SSO corporativo (botão proeminente no topo).
 * Abaixo, um divisor visual ("or") separa o formulário de credenciais locais.
 *
 * Fluxo SSO:
 * 1. Chama POST /identity/auth/oidc/start com provider "default" e returnTo.
 * 2. Redireciona o browser para a authorizationUrl retornada pelo backend.
 *
 * Fluxo local (fallback):
 * - Se o usuário possuir apenas 1 tenant ativo, redireciona ao dashboard.
 * - Se possuir múltiplos tenants, redireciona à tela de seleção.
 */
export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [form, setForm] = useState({ email: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [ssoLoading, setSsoLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }));
  };

  /** Inicia o fluxo OIDC/SSO redirecionando ao identity provider. */
  const handleSsoLogin = async () => {
    setError(null);
    setSsoLoading(true);
    try {
      const returnTo = searchParams.get('returnTo') ?? undefined;
      const { authorizationUrl } = await identityApi.startOidcLogin('default', returnTo);
      window.location.href = authorizationUrl;
    } catch (err) {
      setError(t('auth.ssoError'));
      setSsoLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const result = await login(form.email, form.password);
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

  return (
    <div className="min-h-screen bg-canvas flex items-center justify-center p-4">
      <div className="w-full max-w-md animate-fade-in">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-14 h-14 rounded-xl bg-accent/15 mb-4">
            <span className="text-accent font-bold text-xl">N</span>
          </div>
          <h1 className="text-2xl font-bold text-heading">NexTraceOne</h1>
          <p className="text-muted mt-1">{t('auth.tagline')}</p>
        </div>

        {/* Card */}
        <div className="bg-card rounded-xl shadow-md border border-edge p-8">
          {/* Brand accent stripe */}
          <div className="h-1 brand-gradient rounded-full mb-6" />
          <h2 className="text-lg font-semibold text-heading mb-6">{t('auth.signIn')}</h2>

          {error && (
            <div className="mb-4 rounded-md bg-critical/10 border border-critical/30 px-4 py-3 text-sm text-critical">
              {error}
            </div>
          )}

          {/* SSO — experiência OIDC-first */}
          <div className="mb-6">
            <Button
              type="button"
              variant="secondary"
              size="lg"
              className="w-full"
              loading={ssoLoading}
              onClick={handleSsoLogin}
            >
              <ShieldCheck size={18} />
              {ssoLoading ? t('auth.ssoRedirecting') : t('auth.ssoSignIn')}
            </Button>
            <p className="text-xs text-muted text-center mt-2">{t('auth.ssoDescription')}</p>
          </div>

          {/* Divisor visual entre SSO e credenciais locais */}
          <div className="relative flex items-center mb-6">
            <div className="flex-1 border-t border-edge" />
            <span className="px-3 text-xs text-faded uppercase tracking-wide">
              {t('auth.orDivider')}
            </span>
            <div className="flex-1 border-t border-edge" />
          </div>

          {/* Formulário de credenciais locais (fallback) */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-body mb-1" htmlFor="email">
                {t('auth.email')}
              </label>
              <input
                id="email"
                name="email"
                type="email"
                value={form.email}
                onChange={handleChange}
                required
                placeholder={t('auth.emailPlaceholder')}
                maxLength={254}
                autoComplete="email"
                className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-body mb-1" htmlFor="password">
                {t('auth.password')}
              </label>
              <input
                id="password"
                name="password"
                type="password"
                value={form.password}
                onChange={handleChange}
                required
                placeholder={t('auth.passwordPlaceholder')}
                maxLength={128}
                autoComplete="current-password"
                className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
              />
            </div>

            <Button type="submit" loading={loading} className="w-full mt-2" size="lg">
              {t('auth.signInButton')}
            </Button>
          </form>
        </div>

        <p className="text-center text-xs text-faded mt-6">
          {t('auth.selfHosted')}
        </p>
      </div>
    </div>
  );
}
