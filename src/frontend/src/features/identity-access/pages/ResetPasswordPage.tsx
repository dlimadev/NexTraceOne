import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, CheckCircle2, AlertTriangle } from 'lucide-react';
import { Button, PasswordInput } from '../../../shared/ui';
import { identityApi } from '../api';
import { AuthShell } from '../components/AuthShell';
import { AuthCard } from '../components/AuthCard';
import { AuthFeedback } from '../components/AuthFeedback';
import { resetPasswordSchema, type ResetPasswordFormData } from '../schemas/auth';

type PageState = 'form' | 'success' | 'invalid-token';

/**
 * Página de redefinição de senha — utilizador chega via link com token.
 *
 * Estados:
 * - form: formulário de nova senha + confirmação
 * - success: senha redefinida com sucesso
 * - invalid-token: token ausente, inválido ou expirado
 */
export function ResetPasswordPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const [pageState, setPageState] = useState<PageState>(token ? 'form' : 'invalid-token');
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });

  const onSubmit = async (data: ResetPasswordFormData) => {
    if (!token) return;
    setServerError(null);
    try {
      await identityApi.resetPassword(token, data.newPassword);
      setPageState('success');
    } catch {
      setServerError(t('resetPassword.errorGeneric'));
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
              {t('resetPassword.invalidToken')}
            </h2>
            <p className="text-sm text-muted mb-8">{t('resetPassword.invalidTokenMessage')}</p>
            <div className="flex flex-col gap-3 items-center">
              <Link to="/forgot-password">
                <Button variant="primary" size="md">
                  {t('resetPassword.requestNewLink')}
                </Button>
              </Link>
              <Link
                to="/login"
                className="text-sm text-cyan hover:text-cyan-hover transition-colors font-medium"
              >
                {t('resetPassword.backToLogin')}
              </Link>
            </div>
          </div>
        )}

        {pageState === 'success' && (
          <div className="text-center py-4">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-success/15 mb-5">
              <CheckCircle2 size={28} className="text-success" />
            </div>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('resetPassword.successTitle')}
            </h2>
            <p className="text-sm text-muted mb-8">{t('resetPassword.successMessage')}</p>
            <Link to="/login">
              <Button variant="primary" size="lg">
                {t('resetPassword.goToLogin')}
              </Button>
            </Link>
          </div>
        )}

        {pageState === 'form' && (
          <>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('resetPassword.title')}
            </h2>
            <p className="text-sm text-muted mb-8">{t('resetPassword.subtitle')}</p>

            {serverError && (
              <AuthFeedback variant="error" message={serverError} className="mb-6" />
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
              <PasswordInput
                label={t('resetPassword.newPasswordLabel')}
                placeholder={t('resetPassword.newPasswordPlaceholder')}
                autoComplete="new-password"
                maxLength={128}
                error={errors.newPassword?.message ? t(errors.newPassword.message) : undefined}
                {...register('newPassword')}
              />

              <PasswordInput
                label={t('resetPassword.confirmPasswordLabel')}
                placeholder={t('resetPassword.confirmPasswordPlaceholder')}
                autoComplete="new-password"
                maxLength={128}
                error={
                  errors.confirmPassword?.message ? t(errors.confirmPassword.message) : undefined
                }
                {...register('confirmPassword')}
              />

              <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
                {isSubmitting ? t('resetPassword.resetting') : t('resetPassword.submit')}
              </Button>
            </form>

            <div className="mt-6 text-center">
              <Link
                to="/login"
                className="inline-flex items-center gap-2 text-sm text-cyan hover:text-cyan-hover transition-colors font-medium"
              >
                <ArrowLeft size={16} />
                {t('resetPassword.backToLogin')}
              </Link>
            </div>
          </>
        )}
      </AuthCard>
    </AuthShell>
  );
}
