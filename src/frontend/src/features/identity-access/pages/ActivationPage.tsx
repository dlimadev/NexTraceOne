import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { CheckCircle2, AlertTriangle } from 'lucide-react';
import { Button, PasswordInput } from '../../../shared/ui';
import { identityApi } from '../api';
import { AuthShell } from '../components/AuthShell';
import { AuthCard } from '../components/AuthCard';
import { AuthFeedback } from '../components/AuthFeedback';
import { activationSchema, type ActivationFormData } from '../schemas/auth';

type PageState = 'form' | 'success' | 'invalid-token';

/**
 * Página de ativação de conta / primeiro acesso.
 *
 * O utilizador chega via link de ativação com token.
 * Define a senha para ativar a conta.
 */
export function ActivationPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const [pageState, setPageState] = useState<PageState>(token ? 'form' : 'invalid-token');
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ActivationFormData>({
    resolver: zodResolver(activationSchema),
    defaultValues: { password: '', confirmPassword: '' },
  });

  const onSubmit = async (data: ActivationFormData) => {
    if (!token) return;
    setServerError(null);
    try {
      await identityApi.activateAccount(token, data.password);
      setPageState('success');
    } catch {
      setServerError(t('activation.errorGeneric'));
    }
  };

  return (
    <AuthShell>
      <AuthCard>
        {pageState === 'invalid-token' && (
          <div className="text-center py-4">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-warning/15 mb-5">
              <AlertTriangle size={28} className="text-warning" />
            </div>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('activation.invalidToken')}
            </h2>
            <p className="text-sm text-muted mb-8">{t('activation.invalidTokenMessage')}</p>
            <Link
              to="/login"
              className="text-sm text-cyan hover:text-cyan-hover transition-colors font-medium"
            >
              {t('auth.signInButton')}
            </Link>
          </div>
        )}

        {pageState === 'success' && (
          <div className="text-center py-4">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-success/15 mb-5">
              <CheckCircle2 size={28} className="text-success" />
            </div>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('activation.successTitle')}
            </h2>
            <p className="text-sm text-muted mb-8">{t('activation.successMessage')}</p>
            <Link to="/login">
              <Button variant="primary" size="lg">
                {t('activation.goToLogin')}
              </Button>
            </Link>
          </div>
        )}

        {pageState === 'form' && (
          <>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('activation.title')}
            </h2>
            <p className="text-sm text-muted mb-8">{t('activation.subtitle')}</p>

            {serverError && (
              <AuthFeedback variant="error" message={serverError} className="mb-6" />
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
              <PasswordInput
                label={t('activation.passwordLabel')}
                placeholder={t('activation.passwordPlaceholder')}
                autoComplete="new-password"
                maxLength={128}
                error={errors.password?.message ? t(errors.password.message) : undefined}
                {...register('password')}
              />

              <PasswordInput
                label={t('activation.confirmPasswordLabel')}
                placeholder={t('activation.confirmPasswordPlaceholder')}
                autoComplete="new-password"
                maxLength={128}
                error={
                  errors.confirmPassword?.message ? t(errors.confirmPassword.message) : undefined
                }
                {...register('confirmPassword')}
              />

              <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
                {isSubmitting ? t('activation.activating') : t('activation.submit')}
              </Button>
            </form>
          </>
        )}
      </AuthCard>
    </AuthShell>
  );
}
