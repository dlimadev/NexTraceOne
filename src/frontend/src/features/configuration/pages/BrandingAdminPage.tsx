import { useTranslation } from 'react-i18next';
import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Palette, Image, Type, Globe, Save, CheckCircle2, XCircle,
  RotateCcw, Eye, Monitor, Moon, Sun, LogIn, Shield, Link as LinkIcon,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { IconButton } from '../../../components/IconButton';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Toggle } from '../../../components/Toggle';
import { configurationApi } from '../api/configurationApi';
import type { EffectiveConfigurationDto } from '../types';

/**
 * BrandingAdminPage — página de administração de branding e identidade visual.
 *
 * Permite aos administradores personalizar:
 * - Logo (light/dark variants)
 * - Accent color
 * - Favicon
 * - Welcome message
 * - Footer text
 * - Login page (logo, heading, subheading, background, SSO button text, help text)
 * - Instance name e display settings
 * - Identity protection ("Powered by NexTraceOne" visibility)
 * - Custom navigation links
 *
 * A customização NÃO pode remover a identidade visual do NexTraceOne.
 * Pilar: Platform Customization + Source of Truth + Identity Protection
 * Persona: Platform Admin
 */

interface BrandingField {
  key: string;
  type: 'text' | 'color' | 'url' | 'textarea' | 'toggle';
  icon: React.ReactNode;
  section: 'identity' | 'visual' | 'content' | 'login' | 'protection' | 'links';
}

const BRANDING_FIELDS: BrandingField[] = [
  // Identidade
  { key: 'instance.name', type: 'text', icon: <Globe size={16} />, section: 'identity' },
  { key: 'instance.commercial_name', type: 'text', icon: <Globe size={16} />, section: 'identity' },
  // Página de Login
  { key: 'branding.login_logo_url', type: 'url', icon: <Image size={16} />, section: 'login' },
  { key: 'branding.login_heading', type: 'text', icon: <Type size={16} />, section: 'login' },
  { key: 'branding.login_subheading', type: 'text', icon: <Type size={16} />, section: 'login' },
  { key: 'branding.login_background_url', type: 'url', icon: <Image size={16} />, section: 'login' },
  { key: 'branding.login_sso_button_text', type: 'text', icon: <Shield size={16} />, section: 'login' },
  { key: 'branding.login_help_text', type: 'textarea', icon: <Type size={16} />, section: 'login' },
  // Proteção de Identidade
  { key: 'branding.powered_by_visible', type: 'toggle', icon: <Shield size={16} />, section: 'protection' },
  // Links de Navegação Customizados
  { key: 'platform.custom_links.enabled', type: 'toggle', icon: <LinkIcon size={16} />, section: 'links' },
  { key: 'platform.custom_links.max_items', type: 'text', icon: <LinkIcon size={16} />, section: 'links' },
  { key: 'platform.help.url', type: 'url', icon: <LinkIcon size={16} />, section: 'links' },
  { key: 'platform.help.enabled', type: 'toggle', icon: <LinkIcon size={16} />, section: 'links' },
];

const BRANDING_KEYS = BRANDING_FIELDS.map((f) => f.key);

async function fetchBrandingSettings(): Promise<EffectiveConfigurationDto[]> {
  const settled = await Promise.allSettled(
    BRANDING_KEYS.map((key) =>
      configurationApi.getEffectiveSettings('System', null, key),
    ),
  );
  return settled.flatMap((r) =>
    r.status === 'fulfilled' ? r.value : [],
  );
}

export function BrandingAdminPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['branding-admin-settings'],
    queryFn: fetchBrandingSettings,
  });

  const [values, setValues] = useState<Record<string, string>>({});
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const [previewTheme, setPreviewTheme] = useState<'light' | 'dark'>('dark');

  useEffect(() => {
    if (data) {
      const initial: Record<string, string> = {};
      for (const setting of data) {
        initial[setting.key] = setting.effectiveValue ?? '';
      }
      setValues(initial);
    }
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      await Promise.all(
        BRANDING_KEYS.filter((key) => values[key] !== undefined).map((key) =>
          configurationApi.setConfigurationValue(key, {
            value: values[key],
            scope: 'System',
            changeReason: 'Branding update via admin page',
          }),
        ),
      );
    },
    onSuccess: () => {
      setSaveStatus('saved');
      queryClient.invalidateQueries({ queryKey: ['branding-admin-settings'] });
      queryClient.invalidateQueries({ queryKey: ['branding-settings'] });
      setTimeout(() => setSaveStatus('idle'), 3000);
    },
    onError: () => setSaveStatus('error'),
    onMutate: () => setSaveStatus('saving'),
  });

  const handleChange = useCallback(
    (key: string, value: string) => {
      setValues((prev) => ({ ...prev, [key]: value }));
    },
    [],
  );

  const handleReset = useCallback(
    (key: string) => {
      const original = data?.find((s) => s.key === key);
      setValues((prev) => ({
        ...prev,
        [key]: original?.effectiveValue ?? '',
      }));
    },
    [data],
  );

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const identityFields = BRANDING_FIELDS.filter((f) => f.section === 'identity');
  const loginFields = BRANDING_FIELDS.filter((f) => f.section === 'login');
  const protectionFields = BRANDING_FIELDS.filter((f) => f.section === 'protection');
  const linkFields = BRANDING_FIELDS.filter((f) => f.section === 'links');

  const loginLogoUrl = values['branding.login_logo_url'];
  const accentColor = values['branding.accent_color'] ?? '#3B82F6';

  return (
    <PageContainer>
      <PageHeader
        title={t('branding.admin.title')}
        subtitle={t('branding.admin.description')}
        icon={<Palette size={24} />}
      />

      {/* Aviso de Proteção de Identidade */}
      <div className="mt-6 rounded-lg border border-warning/30 bg-warning/5 px-4 py-3 flex items-start gap-3">
        <Shield size={18} className="text-warning shrink-0 mt-0.5" />
        <div>
          <p className="text-sm font-medium text-heading">{t('branding.admin.identityNotice.title')}</p>
          <p className="text-xs text-muted mt-1">{t('branding.admin.identityNotice.description')}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6 mt-6">
        {/* Coluna 1: Identidade, Login, Proteção, Links */}
        <div className="xl:col-span-2 space-y-6">
          {/* Secção Identidade */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Globe size={18} />
              <span>{t('branding.admin.identity.title')}</span>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                {identityFields.map((field) => (
                  <BrandingFieldEditor
                    key={field.key}
                    field={field}
                    value={values[field.key] ?? ''}
                    onChange={handleChange}
                    onReset={handleReset}
                    t={t}
                  />
                ))}
              </div>
            </CardBody>
          </Card>

          {/* Secção Página de Login */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <LogIn size={18} />
              <span>{t('branding.admin.login.title')}</span>
            </CardHeader>
            <CardBody>
              <p className="text-xs text-muted mb-4">{t('branding.admin.login.description')}</p>
              <div className="space-y-4">
                {loginFields.map((field) => (
                  <BrandingFieldEditor
                    key={field.key}
                    field={field}
                    value={values[field.key] ?? ''}
                    onChange={handleChange}
                    onReset={handleReset}
                    t={t}
                  />
                ))}
              </div>
            </CardBody>
          </Card>

          {/* Secção Proteção de Identidade */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Shield size={18} />
              <span>{t('branding.admin.protection.title')}</span>
            </CardHeader>
            <CardBody>
              <p className="text-xs text-muted mb-4">{t('branding.admin.protection.description')}</p>
              <div className="space-y-4">
                {protectionFields.map((field) => (
                  <BrandingFieldEditor
                    key={field.key}
                    field={field}
                    value={values[field.key] ?? ''}
                    onChange={handleChange}
                    onReset={handleReset}
                    t={t}
                  />
                ))}
              </div>
            </CardBody>
          </Card>

          {/* Secção Links de Navegação Customizados */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <LinkIcon size={18} />
              <span>{t('branding.admin.links.title')}</span>
            </CardHeader>
            <CardBody>
              <p className="text-xs text-muted mb-4">{t('branding.admin.links.description')}</p>
              <div className="space-y-4">
                {linkFields.map((field) => (
                  <BrandingFieldEditor
                    key={field.key}
                    field={field}
                    value={values[field.key] ?? ''}
                    onChange={handleChange}
                    onReset={handleReset}
                    t={t}
                  />
                ))}
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Coluna 2: Preview ao Vivo */}
        <div className="space-y-6">
          {/* Preview da Aplicação */}
          <Card>
            <CardHeader className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Eye size={18} />
                <span>{t('branding.admin.preview.title')}</span>
              </div>
              {/* Botões de toggle do tema do preview — DS IconButton */}
              <div className="flex items-center gap-1">
                <IconButton
                  icon={<Sun size={14} />}
                  label={t('branding.admin.preview.light')}
                  size="sm"
                  variant={previewTheme === 'light' ? 'subtle' : 'ghost'}
                  onClick={() => setPreviewTheme('light')}
                />
                <IconButton
                  icon={<Moon size={14} />}
                  label={t('branding.admin.preview.dark')}
                  size="sm"
                  variant={previewTheme === 'dark' ? 'subtle' : 'ghost'}
                  onClick={() => setPreviewTheme('dark')}
                />
              </div>
            </CardHeader>
            <CardBody>
              {/* Miniatura de Preview da App */}
              <div
                className="rounded-lg border border-edge overflow-hidden"
                style={{
                  background: previewTheme === 'dark' ? '#141419' : '#f5f5fa',
                  color: previewTheme === 'dark' ? '#ebecff' : '#1c1c2e',
                }}
              >
                {/* Mini cabeçalho da sidebar */}
                <div
                  className="px-4 py-3 border-b flex items-center gap-2"
                  style={{
                    borderColor: previewTheme === 'dark' ? 'rgba(255,255,255,0.10)' : 'rgba(15,23,42,0.12)',
                    background: previewTheme === 'dark' ? '#19192c' : '#ffffff',
                  }}
                >
                  <div className="flex items-center gap-1.5">
                    <Monitor size={16} className="text-accent" />
                    <span className="text-xs font-bold">
                      {values['instance.name'] || 'NexTraceOne'}
                    </span>
                  </div>
                </div>

                {/* Mini área de conteúdo */}
                <div className="p-4 space-y-3">
                  {/* Preview de cor de destaque */}
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      className="px-3 py-1.5 rounded-md text-xs text-white font-medium bg-accent"
                      disabled
                    >
                      {t('branding.admin.preview.primaryButton')}
                    </button>
                    <button
                      type="button"
                      className="px-3 py-1.5 rounded-md text-xs font-medium border border-accent text-accent"
                      disabled
                    >
                      {t('branding.admin.preview.secondaryButton')}
                    </button>
                  </div>
                </div>

                {/* Mini rodapé */}
                <div
                  className="border-t px-4 py-2 flex items-center justify-between text-[10px] opacity-60"
                  style={{
                    borderColor: previewTheme === 'dark' ? 'rgba(255,255,255,0.10)' : 'rgba(15,23,42,0.12)',
                  }}
                >
                  <span className="flex-1 text-center">
                    {t('footer.default', { name: values['instance.name'] || 'NexTraceOne' })}
                  </span>
                  {(values['branding.powered_by_visible'] ?? 'true') !== 'false' && (
                    <span className="text-[9px] opacity-50 shrink-0 ml-2">Powered by NexTraceOne</span>
                  )}
                </div>
              </div>
            </CardBody>
          </Card>

          {/* Preview da Página de Login */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <LogIn size={18} />
              <span>{t('branding.admin.preview.loginTitle')}</span>
            </CardHeader>
            <CardBody>
              <div
                className="rounded-lg border border-edge overflow-hidden"
                style={{
                  background: previewTheme === 'dark' ? '#141419' : '#f5f5fa',
                  color: previewTheme === 'dark' ? '#ebecff' : '#1c1c2e',
                }}
              >
                {/* Área de fundo do login */}
                <div
                  className="px-4 py-6 flex flex-col items-center gap-3"
                  style={{
                    backgroundImage: values['branding.login_background_url']
                      ? `url(${values['branding.login_background_url']})`
                      : undefined,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                  }}
                >
                  {/* Logo do login */}
                  {loginLogoUrl ? (
                    <img
                      src={loginLogoUrl}
                      alt="Login logo preview"
                      className="h-10 w-auto max-w-[140px] object-contain"
                      onError={(e) => {
                        (e.target as HTMLImageElement).style.display = 'none';
                      }}
                    />
                  ) : (
                    <div className="flex items-center gap-1.5">
                      <Monitor size={20} style={{ color: accentColor }} />
                    </div>
                  )}

                  {/* Heading do login */}
                  <span className="text-xs font-semibold text-center">
                    {values['branding.login_heading'] || t('auth.welcomeTitle')}
                  </span>

                  {/* Subheading do login */}
                  <span className="text-[10px] opacity-70 text-center">
                    {values['branding.login_subheading'] || t('auth.signInSubtitle')}
                  </span>

                  {/* Mini preview do formulário */}
                  <div className="w-full space-y-2 mt-2">
                    <div
                      className="h-7 rounded-md border w-full"
                      style={{ borderColor: previewTheme === 'dark' ? 'rgba(255,255,255,0.12)' : 'rgba(15,23,42,0.15)' }}
                    />
                    <div
                      className="h-7 rounded-md border w-full"
                      style={{ borderColor: previewTheme === 'dark' ? 'rgba(255,255,255,0.12)' : 'rgba(15,23,42,0.15)' }}
                    />
                    <div
                      className="h-7 rounded-md w-full text-center text-[10px] text-white font-medium flex items-center justify-center bg-accent"
                    >
                      Sign In
                    </div>
                  </div>

                  {/* Preview do botão SSO */}
                  {values['branding.login_sso_button_text'] && (
                    <div
                      className="h-7 rounded-md border w-full text-center text-[10px] flex items-center justify-center"
                      style={{ borderColor: previewTheme === 'dark' ? 'rgba(255,255,255,0.12)' : 'rgba(15,23,42,0.15)' }}
                    >
                      {values['branding.login_sso_button_text']}
                    </div>
                  )}

                  {/* Texto de ajuda */}
                  {values['branding.login_help_text'] && (
                    <span className="text-[9px] opacity-50 text-center">
                      {values['branding.login_help_text']}
                    </span>
                  )}
                </div>
              </div>
            </CardBody>
          </Card>
        </div>
      </div>

      {/* Botão Salvar */}
      <div className="mt-6 flex items-center gap-3">
        <Button
          variant="primary"
          onClick={() => saveMutation.mutate()}
          disabled={saveStatus === 'saving'}
          icon={<Save size={16} />}
        >
          {saveStatus === 'saving'
            ? t('common.saving', 'Saving...')
            : t('branding.admin.save')}
        </Button>
        {saveStatus === 'saved' && (
          <span className="flex items-center gap-1 text-success text-sm">
            <CheckCircle2 size={14} /> {t('branding.admin.saved')}
          </span>
        )}
        {saveStatus === 'error' && (
          <span className="flex items-center gap-1 text-critical text-sm">
            <XCircle size={14} /> {t('branding.admin.error')}
          </span>
        )}
      </div>
    </PageContainer>
  );
}

/** Editor de campo individual para parâmetros de branding. */
function BrandingFieldEditor({
  field,
  value,
  onChange,
  onReset,
  t,
}: {
  field: BrandingField;
  value: string;
  onChange: (key: string, value: string) => void;
  onReset: (key: string) => void;
  t: (key: string, opts?: Record<string, string>) => string;
}) {
  const label = t(`config.${field.key}.label`, { defaultValue: field.key });
  const description = t(`config.${field.key}.description`, { defaultValue: '' });

  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between">
        <label className="flex items-center gap-2 text-sm font-medium text-heading">
          {field.icon}
          {label}
        </label>
        {/* Botão de reset — DS IconButton ghost */}
        <IconButton
          icon={<RotateCcw size={12} />}
          label={t('common.reset', { defaultValue: 'Reset' })}
          size="sm"
          variant="ghost"
          onClick={() => onReset(field.key)}
        />
      </div>

      {description && (
        <p className="text-xs text-muted">{description}</p>
      )}

      {field.type === 'toggle' ? (
        /* Toggle DS para campos booleanos */
        <Toggle
          checked={value === 'true' || value === ''}
          onChange={(checked) => onChange(field.key, checked ? 'true' : 'false')}
          size="sm"
        />
      ) : field.type === 'color' ? (
        <div className="flex items-center gap-3">
          {/* Input de cor nativo mantido — é um seletor de cor funcional, não estilização */}
          <input
            type="color"
            value={value || '#3B82F6'}
            onChange={(e) => onChange(field.key, e.target.value)}
            className="h-9 w-14 rounded border border-edge cursor-pointer"
          />
          {/* TextField DS para edição manual do valor hex */}
          <TextField
            type="text"
            value={value}
            onChange={(e) => onChange(field.key, e.target.value)}
            placeholder={t('configuration.branding.placeholder.colorHex', { defaultValue: '#3B82F6' })}
            size="sm"
            maxLength={7}
            className="flex-1"
          />
        </div>
      ) : field.type === 'textarea' ? (
        /* TextArea DS para campos de texto longo */
        <TextArea
          value={value}
          onChange={(e) => onChange(field.key, e.target.value)}
          placeholder={t(`config.${field.key}.placeholder`, { defaultValue: '' })}
          rows={3}
          maxLength={500}
        />
      ) : (
        /* TextField DS para campos de texto simples e URL */
        <TextField
          type={field.type === 'url' ? 'url' : 'text'}
          value={value}
          onChange={(e) => onChange(field.key, e.target.value)}
          placeholder={t(`config.${field.key}.placeholder`, { defaultValue: '' })}
          size="sm"
        />
      )}
    </div>
  );
}

export default BrandingAdminPage;
