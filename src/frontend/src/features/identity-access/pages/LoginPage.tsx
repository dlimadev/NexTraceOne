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
import { AuthDivider } from '../components/AuthDivider';
import { AuthFeedback } from '../components/AuthFeedback';
import { loginSchema, type LoginFormData } from '../schemas/auth';

const DEFAULT_LOGO_ICON = '/brand/logo-icon.svg';

/**
 * Página de login — coluna única centrada no estilo Betterstack.
 *
 * Layout minimalista: logo, heading "Welcome back", prompt de signup,
 * formulário email/password, botão primário full-width, divisor "or",
 * botão SSO secundário e rodapé legal. Sem painel lateral.
 *
 * Mantém o fluxo real (email + password + SSO) — não há magic link no backend.
 *
 * Suporta customização via branding parameters:
 * - branding.login_logo_url — logo custom
 * - branding.login_heading — heading custom
 * - branding.login_subheading — subheading custom
 * - branding.login_sso_button_text — texto do botão SSO
 * - branding.login_help_text — texto de ajuda abaixo do form
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
      {/* Logo + heading + signup prompt — centrados */}
      <div className="flex flex-col items-center text-center mb-8">
        {loginLogoUrl ? (
          <img
            src={loginLogoUrl}
            alt={t('auth.logoAlt', 'Logo')}
            className="h-11 w-auto max-w-[180px] object-contain"
            onError={(e) => {
              (e.target as HTMLImageElement).src = DEFAULT_LOGO_ICON;
            }}
          />
        ) : (
          <img src={DEFAULT_LOGO_ICON} alt="NexTraceOne" className="h-11 w-auto" />
        )}

        <h1 className="text-3xl font-bold text-heading tracking-tight mt-6">
          {loginHeading || t('auth.welcomeTitle')}
        </h1>
        <p className="text-sm text-muted mt-2">
          {loginSubheading || (
            <>
              {t('auth.noAccount', "Don't have an account?")}{' '}
              <Link to="/signup" className="text-accent hover:text-accent-hover font-medium transition-colors">
                {t('auth.createWorkspace', 'Create your workspace')}
              </Link>
            </>
          )}
        </p>
      </div>

      {serverError && <AuthFeedback variant="error" message={serverError} className="mb-5" />}

      {/* Credentials form */}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 text-left" noValidate>
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

        <PasswordInput
          label={t('auth.password')}
          placeholder={t('auth.passwordPlaceholder')}
          autoComplete="current-password"
          maxLength={128}
          error={errors.password?.message ? t(errors.password.message) : undefined}
          {...register('password')}
        />

        <div className="flex items-center justify-between">
          <Checkbox label={t('auth.rememberMe')} />
          <Link
            to="/forgot-password"
            className="text-xs text-accent hover:text-accent-hover transition-colors"
          >
            {t('auth.forgotPasswordLink')}
          </Link>
        </div>

        <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
          {t('auth.signInButton')}
        </Button>
      </form>

      {/* SSO */}
      <AuthDivider labelKey="auth.orContinueWith" />

      <Button
        type="button"
        variant="secondary"
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

      {loginHelpText && (
        <p className="text-center text-xs text-faded mt-6">{loginHelpText}</p>
      )}

      {/* Rodapé legal */}
      <p className="text-center text-xs text-faded mt-10 leading-relaxed">
        {t('auth.legalAcknowledgePrefix', 'You acknowledge that you read, and agree to our')}{' '}
        <span className="text-body font-medium">{t('auth.termsOfService', 'Terms of Service')}</span>
        {' '}{t('auth.legalAnd', 'and our')}{' '}
        <span className="text-body font-medium">{t('auth.privacyPolicy', 'Privacy Policy')}</span>.
      </p>
    </AuthShell>
  );
}
