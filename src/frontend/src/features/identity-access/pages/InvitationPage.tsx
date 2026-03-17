import { useState, useEffect } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { CheckCircle2, AlertTriangle, Users } from 'lucide-react';
import { Button, PasswordInput, Loader } from '../../../shared/ui';
import { identityApi } from '../api';
import { AuthShell } from '../components/AuthShell';
import { AuthCard } from '../components/AuthCard';
import { AuthFeedback } from '../components/AuthFeedback';
import { invitationSchema, type InvitationFormData } from '../schemas/auth';

type PageState = 'loading' | 'form' | 'success' | 'invalid-token';

interface InvitationDetails {
  email: string;
  organizationName: string;
  roleName: string;
  expiresAt: string;
}

/**
 * Página de aceite de convite — utilizador aceita convite e define senha.
 *
 * Fluxo:
 * 1. Carrega detalhes do convite via token
 * 2. Exibe organização, email e papel
 * 3. Utilizador define senha e aceita
 */
export function InvitationPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const [pageState, setPageState] = useState<PageState>(token ? 'loading' : 'invalid-token');
  const [invitation, setInvitation] = useState<InvitationDetails | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<InvitationFormData>({
    resolver: zodResolver(invitationSchema),
    defaultValues: { password: '', confirmPassword: '' },
  });

  useEffect(() => {
    if (!token) return;
    let cancelled = false;

    identityApi
      .getInvitationDetails(token)
      .then((data) => {
        if (!cancelled) {
          setInvitation(data);
          setPageState('form');
        }
      })
      .catch(() => {
        if (!cancelled) setPageState('invalid-token');
      });

    return () => {
      cancelled = true;
    };
  }, [token]);

  const onSubmit = async (data: InvitationFormData) => {
    if (!token) return;
    setServerError(null);
    try {
      await identityApi.acceptInvitation(token, data.password);
      setPageState('success');
    } catch {
      setServerError(t('invitation.errorGeneric'));
    }
  };

  return (
    <AuthShell>
      <AuthCard>
        {pageState === 'loading' && (
          <div className="flex items-center justify-center py-16">
            <Loader size="lg" />
          </div>
        )}

        {pageState === 'invalid-token' && (
          <div className="text-center py-4">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-warning/15 mb-5">
              <AlertTriangle size={28} className="text-warning" />
            </div>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('invitation.invalidToken')}
            </h2>
            <p className="text-sm text-muted mb-8">{t('invitation.invalidTokenMessage')}</p>
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
              {t('invitation.successTitle')}
            </h2>
            <p className="text-sm text-muted mb-8">
              {t('invitation.successMessage', { organization: invitation?.organizationName })}
            </p>
            <Link to="/login">
              <Button variant="primary" size="lg">
                {t('invitation.goToLogin')}
              </Button>
            </Link>
          </div>
        )}

        {pageState === 'form' && invitation && (
          <>
            <div className="text-center mb-8">
              <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-accent/15 mb-5">
                <Users size={28} className="text-accent" />
              </div>
              <h2 className="text-xl font-semibold text-heading mb-2">
                {t('invitation.title')}
              </h2>
              <p className="text-sm text-muted">
                {t('invitation.subtitle', { organization: invitation.organizationName })}
              </p>
            </div>

            {/* Invitation context */}
            <div className="rounded-lg bg-elevated border border-edge p-4 mb-6 space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-faded">{t('invitation.emailLabel')}</span>
                <span className="text-heading font-medium">{invitation.email}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-faded">{t('invitation.roleLabel')}</span>
                <span className="text-heading font-medium">{invitation.roleName}</span>
              </div>
            </div>

            {serverError && (
              <AuthFeedback variant="error" message={serverError} className="mb-6" />
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
              <PasswordInput
                label={t('invitation.passwordLabel')}
                placeholder={t('invitation.passwordPlaceholder')}
                autoComplete="new-password"
                maxLength={128}
                error={errors.password?.message ? t(errors.password.message) : undefined}
                {...register('password')}
              />

              <PasswordInput
                label={t('invitation.confirmPasswordLabel')}
                placeholder={t('invitation.confirmPasswordPlaceholder')}
                autoComplete="new-password"
                maxLength={128}
                error={
                  errors.confirmPassword?.message ? t(errors.confirmPassword.message) : undefined
                }
                {...register('confirmPassword')}
              />

              <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
                {isSubmitting ? t('invitation.accepting') : t('invitation.accept')}
              </Button>
            </form>
          </>
        )}
      </AuthCard>
    </AuthShell>
  );
}
