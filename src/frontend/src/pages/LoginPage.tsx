import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '../components/Button';
import { resolveApiError } from '../utils/apiErrors';

/**
 * Página de login — autentica com email e senha sem exigir GUID de tenant.
 *
 * Após autenticação bem-sucedida:
 * - Se o usuário possuir apenas 1 tenant ativo, redireciona ao dashboard.
 * - Se possuir múltiplos tenants, redireciona à tela de seleção.
 */
export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState({ email: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }));
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
