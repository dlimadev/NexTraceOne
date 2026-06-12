import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, Building2, CheckCircle2, Mail, User } from 'lucide-react';
import { Button, TextField } from '../../../shared/ui';
import { identityApi } from '../api';
import { AuthShell } from '../components/AuthShell';
import { AuthCard } from '../components/AuthCard';
import { AuthFeedback } from '../components/AuthFeedback';
import { signUpSchema, type SignUpFormData } from '../schemas/auth';
import { resolveApiError } from '../../../utils/apiErrors';

/**
 * Página de cadastro self-service — cria tenant + administrador + trial 14 dias.
 *
 * Fluxo:
 * 1. Visitante preenche empresa, workspace (slug), nome e email
 * 2. Backend provisiona tenant com licença Trial e envia email de ativação
 * 3. Usuário define a senha na ActivationPage via link recebido
 */
export function SignupPage() {
  const { t } = useTranslation();
  const [submitted, setSubmitted] = useState(false);
  const [submittedEmail, setSubmittedEmail] = useState('');
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<SignUpFormData>({
    resolver: zodResolver(signUpSchema),
    defaultValues: { companyName: '', slug: '', email: '', firstName: '', lastName: '' },
  });

  const suggestSlug = (companyName: string) => {
    const slug = companyName
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
    if (slug) setValue('slug', slug, { shouldValidate: false });
  };

  const onSubmit = async (data: SignUpFormData) => {
    setServerError(null);
    try {
      await identityApi.signUp(data);
      setSubmittedEmail(data.email);
      setSubmitted(true);
    } catch (error) {
      setServerError(resolveApiError(error));
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
              {t('signup.successTitle', 'Check your email')}
            </h2>
            <p className="text-sm text-muted mb-8 max-w-sm mx-auto">
              {t('signup.successMessage', 'We sent an activation link to {{email}}. Open it to set your password and access your workspace.', { email: submittedEmail })}
            </p>
            <Link
              to="/login"
              className="inline-flex items-center gap-2 text-sm text-accent hover:text-heading transition-colors font-medium"
            >
              <ArrowLeft size={16} />
              {t('signup.backToLogin', 'Back to sign in')}
            </Link>
          </div>
        ) : (
          <>
            <h2 className="text-xl font-semibold text-heading mb-2">
              {t('signup.title', 'Create your workspace')}
            </h2>
            <p className="text-sm text-muted mb-8">
              {t('signup.subtitle', 'Start a 14-day free trial. No credit card required.')}
            </p>

            {serverError && (
              <AuthFeedback variant="error" message={serverError} className="mb-6" />
            )}

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
              <TextField
                label={t('signup.companyLabel', 'Company name')}
                placeholder={t('signup.companyPlaceholder', 'Acme Corp')}
                maxLength={256}
                leadingIcon={<Building2 size={16} />}
                error={errors.companyName?.message ? t(errors.companyName.message) : undefined}
                {...register('companyName', {
                  onBlur: (e) => suggestSlug(e.target.value),
                })}
              />

              <TextField
                label={t('signup.slugLabel', 'Workspace URL')}
                placeholder={t('signup.slugPlaceholder', 'acme-corp')}
                maxLength={128}
                spellCheck={false}
                error={errors.slug?.message ? t(errors.slug.message) : undefined}
                {...register('slug')}
              />

              <div className="grid grid-cols-2 gap-4">
                <TextField
                  label={t('signup.firstNameLabel', 'First name')}
                  maxLength={100}
                  leadingIcon={<User size={16} />}
                  error={errors.firstName?.message ? t(errors.firstName.message) : undefined}
                  {...register('firstName')}
                />
                <TextField
                  label={t('signup.lastNameLabel', 'Last name')}
                  maxLength={100}
                  error={errors.lastName?.message ? t(errors.lastName.message) : undefined}
                  {...register('lastName')}
                />
              </div>

              <TextField
                label={t('signup.emailLabel', 'Work email')}
                type="email"
                placeholder={t('signup.emailPlaceholder', 'you@company.com')}
                autoComplete="email"
                spellCheck={false}
                maxLength={254}
                leadingIcon={<Mail size={16} />}
                error={errors.email?.message ? t(errors.email.message) : undefined}
                {...register('email')}
              />

              <Button type="submit" loading={isSubmitting} className="w-full" size="lg">
                {isSubmitting
                  ? t('signup.submitting', 'Creating workspace…')
                  : t('signup.submit', 'Create workspace')}
              </Button>
            </form>

            <div className="mt-6 text-center">
              <Link
                to="/login"
                className="inline-flex items-center gap-2 text-sm text-accent hover:text-heading transition-colors font-medium"
              >
                <ArrowLeft size={16} />
                {t('signup.backToLogin', 'Back to sign in')}
              </Link>
            </div>
          </>
        )}
      </AuthCard>
    </AuthShell>
  );
}
