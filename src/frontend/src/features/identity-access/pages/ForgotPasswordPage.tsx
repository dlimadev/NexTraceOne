import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, Mail, CheckCircle2 } from 'lucide-react';
import { Button, TextField } from '../../../shared/ui';
import { identityApi } from '../api';
import { AuthShell } from '../components/AuthShell';
import { AuthCard } from '../components/AuthCard';
import { AuthFeedback } from '../components/AuthFeedback';
import { forgotPasswordSchema, type ForgotPasswordFormData } from '../schemas/auth';

/**
 * Página de recuperação de senha — solicita link de reset por email.
 *
 * Fluxo:
 * 1. Utilizador insere email
 * 2. Sistema envia link de reset (sem revelar se o email existe)
 * 3. Feedback de sucesso sempre exibido por segurança
 */
export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const [submitted, setSubmitted] = useState(false);
  const [submittedEmail, setSubmittedEmail] = useState('');
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setServerError(null);
    try {
      await identityApi.forgotPassword(data.email);
      setSubmittedEmail(data.email);
      setSubmitted(true);
    } catch {
      setServerError(t('forgotPassword.errorGeneric'));
    }
  };

  return (
    <AuthShell>
      <AuthCard>
        {submitted ? (
          <div className="text-center py-4">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-success/15 mb-5">
              <CheckCircle2 size={28} className="text-success" />
            </div>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('forgotPassword.successTitle')}
            </h2>
            <p className="text-sm text-muted mb-8 max-w-sm mx-auto">
              {t('forgotPassword.successMessage', { email: submittedEmail })}
            </p>
            <Link
              to="/login"
              className="inline-flex items-center gap-2 text-sm text-cyan hover:text-cyan-hover transition-colors font-medium"
            >
              <ArrowLeft size={16} />
              {t('forgotPassword.backToLogin')}
            </Link>
          </div>
        ) : (
          <>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('forgotPassword.title')}
            </h2>
            <p className="text-sm text-muted mb-8">{t('forgotPassword.subtitle')}</p>

            {serverError && (
              <AuthFeedback variant="error" message={serverError} className="mb-6" />
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
              <TextField
                label={t('forgotPassword.emailLabel')}
                type="email"
                placeholder={t('forgotPassword.emailPlaceholder')}
                autoComplete="username"
                spellCheck={false}
                maxLength={254}
                leadingIcon={<Mail size={16} />}
                error={errors.email?.message ? t(errors.email.message) : undefined}
                {...register('email')}
              />

              <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
                {isSubmitting ? t('forgotPassword.sending') : t('forgotPassword.submit')}
              </Button>
            </form>

            <div className="mt-6 text-center">
              <Link
                to="/login"
                className="inline-flex items-center gap-2 text-sm text-cyan hover:text-cyan-hover transition-colors font-medium"
              >
                <ArrowLeft size={16} />
                {t('forgotPassword.backToLogin')}
              </Link>
            </div>
          </>
        )}
      </AuthCard>
    </AuthShell>
  );
}
