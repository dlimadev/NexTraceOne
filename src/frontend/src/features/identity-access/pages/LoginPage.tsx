import { useState, useCallback } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ShieldCheck, Mail } from 'lucide-react';
import { useAuth } from '../../../contexts/AuthContext';
import { useBranding } from '../../../contexts/BrandingContext';
import { Button, TextField, PasswordInput, Checkbox } from '../../../shared/ui';
import { identityApi } from '../api';
import { resolveApiError } from '../../../utils/apiErrors';
import { AuthShell } from '../components/AuthShell';
import { AuthCard } from '../components/AuthCard';
import { AuthDivider } from '../components/AuthDivider';
import { AuthFeedback } from '../components/AuthFeedback';
import { loginSchema, type LoginFormData } from '../schemas/auth';

/**
 * Página de login — Auth Shell enterprise inspirada no NexLink template.
 *
 * Layout: split 50/50 com ilustração à esquerda e card à direita.
 * Logo centrado no card, heading "Welcome to NexTraceOne", campos de
 * email/password, remember me, forgot password, botão de login, SSO abaixo.
 * Pill theme toggle no canto superior direito (via AuthShell).
 *
 * Agora suporta customização via branding parameters:
 * - branding.login_logo_url — logo custom na auth card
 * - branding.login_heading — heading custom
 * - branding.login_subheading — subheading custom
 * - branding.login_sso_button_text — texto do botão SSO
 * - branding.login_help_text — texto de ajuda abaixo do form
 * A identidade visual do NexTraceOne é preservada no painel esquerdo.
 */
export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const {
    loginLogoUrl, loginHeading, loginSubheading,
    loginSsoButtonText, loginHelpText,
  } = useBranding();

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
        {/* Logo centrado — custom login logo ou default globe icon */}
        <div className="flex justify-center mb-6">
          {loginLogoUrl ? (
            <img
              src={loginLogoUrl}
              alt={t('auth.logoAlt', 'Logo')}
              className="h-12 w-auto max-w-[200px] object-contain"
              onError={(e) => {
                // Fallback to default logo on broken URL
                (e.target as HTMLImageElement).src = '/brand/logo-icon.svg';
              }}
            />
          ) : (
            <img src="/brand/logo-icon.svg" alt="NexTraceOne" className="h-12 w-auto" />
          )}
        </div>

        {/* Heading — custom or default "Welcome to NexTraceOne" */}
        <div className="text-center mb-8">
          <h2 className="text-xl font-semibold text-heading mb-1">
            {loginHeading || t('auth.welcomeTitle')}
          </h2>
          <p className="text-sm text-muted">
            {loginSubheading || t('auth.signInSubtitle')}
          </p>
        </div>

        {serverError && <AuthFeedback variant="error" message={serverError} className="mb-6" />}

        {/* Credentials form */}
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
          </div>

          {/* Remember me + Forgot password row */}
          <div className="flex items-center justify-between">
            <Checkbox label={t('auth.rememberMe')} />
            <Link
              to="/forgot-password"
              className="text-xs text-cyan hover:text-cyan-hover transition-colors"
            >
              {t('auth.forgotPasswordLink')}
            </Link>
          </div>

          <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
            {t('auth.signInButton')}
          </Button>
        </form>

        {/* SSO section */}
        <AuthDivider labelKey="auth.orContinueWith" />

        <Button
          type="button"
          variant="institutional"
          size="lg"
          className="w-full"
          loading={ssoLoading}
          onClick={handleSsoLogin}
        >
          <ShieldCheck size={18} />
          {ssoLoading
            ? t('auth.ssoRedirecting')
            : (loginSsoButtonText || t('auth.ssoSignIn'))}
        </Button>
        <p className="text-xs text-muted text-center mt-2.5">{t('auth.ssoDescription')}</p>

        {/* Help link — custom or default */}
        <p className="text-center text-xs text-faded mt-6">
          {loginHelpText ? (
            <span>{loginHelpText}</span>
          ) : (
            <>
              {t('auth.needHelp')}{' '}
              <button type="button" className="text-cyan hover:text-cyan-hover cursor-pointer transition-colors bg-transparent border-none p-0 text-xs">
                {t('auth.contactSupport')}
              </button>
            </>
          )}
        </p>
      </AuthCard>

      {/* Footer trust signals */}
      <div className="mt-6 flex items-center justify-center gap-3 text-xs text-faded flex-wrap">
        <span>{t('auth.selfHosted')}</span>
      </div>
    </AuthShell>
  );
}
