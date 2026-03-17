import { z } from 'zod';

/**
 * Schemas Zod para formulários de autenticação.
 * Mensagens de validação referem-se a chaves i18n — tradução aplicada no componente.
 */

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'validation.required')
    .email('validation.invalidEmail'),
  password: z
    .string()
    .min(1, 'validation.required'),
});
export type LoginFormData = z.infer<typeof loginSchema>;

export const forgotPasswordSchema = z.object({
  email: z
    .string()
    .min(1, 'validation.required')
    .email('validation.invalidEmail'),
});
export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

const passwordRules = z
  .string()
  .min(8, 'resetPassword.passwordTooShort')
  .regex(/[A-Z]/, 'resetPassword.passwordRequiresUppercase')
  .regex(/[0-9]/, 'resetPassword.passwordRequiresNumber')
  .regex(/[^A-Za-z0-9]/, 'resetPassword.passwordRequiresSpecial');

export const resetPasswordSchema = z
  .object({
    newPassword: passwordRules,
    confirmPassword: z.string().min(1, 'validation.required'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'resetPassword.passwordMismatch',
    path: ['confirmPassword'],
  });
export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

export const activationSchema = z
  .object({
    password: passwordRules,
    confirmPassword: z.string().min(1, 'validation.required'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'resetPassword.passwordMismatch',
    path: ['confirmPassword'],
  });
export type ActivationFormData = z.infer<typeof activationSchema>;

export const mfaSchema = z.object({
  code: z
    .string()
    .min(6, 'validation.required')
    .max(6, 'validation.required')
    .regex(/^\d{6}$/, 'mfa.invalidCode'),
});
export type MfaFormData = z.infer<typeof mfaSchema>;

export const invitationSchema = z
  .object({
    password: passwordRules,
    confirmPassword: z.string().min(1, 'validation.required'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'resetPassword.passwordMismatch',
    path: ['confirmPassword'],
  });
export type InvitationFormData = z.infer<typeof invitationSchema>;
