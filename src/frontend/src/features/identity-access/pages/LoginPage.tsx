import { useState, useCallback } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ShieldCheck, Mail } from 'lucide-react';
import { useAuth } from '../../../contexts/AuthContext';
import { Button, TextField, PasswordInput } from '../../../shared/ui';
import { identityApi } from '../api';
import { resolveApiError } from '../../../utils/apiErrors';
import { AuthShell } from '../components/AuthShell';
import { AuthCard } from '../components/AuthCard';
import { AuthDivider } from '../components/AuthDivider';
import { AuthFeedback } from '../components/AuthFeedback';
import { loginSchema, type LoginFormData } from '../schemas/auth';

/**
 * Página de login — Auth Shell enterprise (DESIGN-SYSTEM.md §4.2, DESIGN.md §9.1)
 *
 * Usa AuthShell (split-layout: hero 55% + card 45%).
 * react-hook-form + zod para validação tipada.
 * Design system components: TextField, PasswordInput, Button.
 */
export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [ssoLoading, setSsoLoading] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  const clearSensitiveState = useCallback(() => {
    reset({ email: '', password: '' });
  }, [reset]);

  const handleSsoLogin = async () => {
    setServerError(null);
    setSsoLoading(true);
    try {
      const returnTo = searchParams.get('returnTo') ?? undefined;
      const { authorizationUrl } = await identityApi.startOidcLogin('default', returnTo);
      window.location.href = authorizationUrl;
    } catch {
      setServerError(t('auth.ssoError'));
      setSsoLoading(false);
    }
  };

  const onSubmit = async (data: LoginFormData) => {
    setServerError(null);
    try {
      const result = await login(data.email, data.password);
      clearSensitiveState();
      if (result === 'select-tenant') {
        navigate('/select-tenant');
      } else {
        navigate('/');
      }
    } catch (err) {
      setServerError(resolveApiError(err));
    }
  };

  return (
    <AuthShell>
      <AuthCard>
        <h2 className="text-xl font-semibold text-heading mb-1">{t('auth.signIn')}</h2>
        <p className="text-sm text-muted mb-8">{t('auth.signInSubtitle')}</p>

        {serverError && <AuthFeedback variant="error" message={serverError} className="mb-6" />}

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

        <AuthDivider />

        {/* Credentials form — react-hook-form + zod */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <TextField
            label={t('auth.email')}
            type="email"
            placeholder={t('auth.emailPlaceholder')}
            autoComplete="username"
            spellCheck={false}
            maxLength={254}
            leadingIcon={<Mail size={16} />}
            error={errors.email?.message ? t(errors.email.message) : undefined}
            {...register('email')}
          />

          <div>
            <PasswordInput
              label={t('auth.password')}
              placeholder={t('auth.passwordPlaceholder')}
              autoComplete="current-password"
              maxLength={128}
              error={errors.password?.message ? t(errors.password.message) : undefined}
              {...register('password')}
            />
            <div className="flex justify-end mt-2">
              <Link
                to="/forgot-password"
                className="text-xs text-cyan hover:text-cyan-hover transition-colors"
              >
                {t('auth.forgotPasswordLink')}
              </Link>
            </div>
          </div>

          <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
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
      </AuthCard>

      {/* Footer trust signals */}
      <div className="mt-6 flex items-center justify-center gap-3 text-xs text-faded flex-wrap">
        <span>{t('auth.selfHosted')}</span>
      </div>
    </AuthShell>
  );
}
