import { useState } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ShieldCheck, ArrowLeft } from 'lucide-react';
import { Button, TextField } from '../../../shared/ui';
import { identityApi } from '../api';
import { AuthShell } from '../components/AuthShell';
import { AuthCard } from '../components/AuthCard';
import { AuthFeedback } from '../components/AuthFeedback';
import { mfaSchema, type MfaFormData } from '../schemas/auth';

/**
 * Página de verificação MFA / 2FA.
 *
 * O utilizador insere o código de 6 dígitos do autenticador.
 * O sessionId vem do query param (passado pelo fluxo de login quando MFA é requerido).
 */
export function MfaPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const sessionId = searchParams.get('session') ?? '';

  const [serverError, setServerError] = useState<string | null>(null);
  const [resendFeedback, setResendFeedback] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<MfaFormData>({
    resolver: zodResolver(mfaSchema),
    defaultValues: { code: '' },
  });

  const onSubmit = async (data: MfaFormData) => {
    setServerError(null);
    setResendFeedback(null);
    try {
      await identityApi.verifyMfa(data.code, sessionId);
      navigate('/');
    } catch {
      setServerError(t('mfa.invalidCode'));
    }
  };

  const handleResend = async () => {
    setResendFeedback(null);
    setServerError(null);
    try {
      await identityApi.resendMfaCode(sessionId);
      setResendFeedback(t('mfa.resendSuccess'));
    } catch {
      setServerError(t('mfa.errorGeneric'));
    }
  };

  return (
    <AuthShell>
      <AuthCard>
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-accent/15 mb-5">
            <ShieldCheck size={28} className="text-accent" />
          </div>
          <h2 className="text-xl font-semibold text-heading mb-2">{t('mfa.title')}</h2>
          <p className="text-sm text-muted">{t('mfa.subtitle')}</p>
        </div>

        {serverError && <AuthFeedback variant="error" message={serverError} className="mb-6" />}
        {resendFeedback && (
          <AuthFeedback variant="success" message={resendFeedback} className="mb-6" />
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <TextField
            label={t('mfa.codeLabel')}
            type="text"
            inputMode="numeric"
            placeholder={t('mfa.codePlaceholder')}
            autoComplete="one-time-code"
            maxLength={6}
            className="text-center text-lg tracking-[0.3em] font-mono"
            error={errors.code?.message ? t(errors.code.message) : undefined}
            {...register('code')}
          />

          <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
            {isSubmitting ? t('mfa.verifying') : t('mfa.submit')}
          </Button>
        </form>

        <div className="mt-6 flex flex-col items-center gap-3">
          <button
            type="button"
            onClick={handleResend}
            className="text-sm text-cyan hover:text-cyan-hover transition-colors font-medium"
          >
            {t('mfa.resend')}
          </button>
          <Link
            to="/login"
            className="inline-flex items-center gap-2 text-sm text-faded hover:text-body transition-colors"
          >
            <ArrowLeft size={14} />
            {t('mfa.backToLogin')}
          </Link>
        </div>
      </AuthCard>
    </AuthShell>
  );
}
