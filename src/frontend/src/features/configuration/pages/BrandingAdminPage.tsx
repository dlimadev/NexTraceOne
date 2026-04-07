import { useTranslation } from 'react-i18next';
import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Palette, Image, Type, Globe, Save, CheckCircle, XCircle,
  RotateCcw, Eye, Monitor, Moon, Sun, LogIn, Shield, Link as LinkIcon,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
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
  // Identity
  { key: 'instance.name', type: 'text', icon: <Globe size={16} />, section: 'identity' },
  { key: 'instance.commercial_name', type: 'text', icon: <Globe size={16} />, section: 'identity' },
  // Visual
  { key: 'branding.logo_url', type: 'url', icon: <Image size={16} />, section: 'visual' },
  { key: 'branding.logo_dark_url', type: 'url', icon: <Image size={16} />, section: 'visual' },
  { key: 'branding.accent_color', type: 'color', icon: <Palette size={16} />, section: 'visual' },
  { key: 'branding.favicon_url', type: 'url', icon: <Image size={16} />, section: 'visual' },
  // Content
  { key: 'branding.welcome_message', type: 'textarea', icon: <Type size={16} />, section: 'content' },
  { key: 'branding.footer_text', type: 'textarea', icon: <Type size={16} />, section: 'content' },
  // Login Page
  { key: 'branding.login_logo_url', type: 'url', icon: <Image size={16} />, section: 'login' },
  { key: 'branding.login_heading', type: 'text', icon: <Type size={16} />, section: 'login' },
  { key: 'branding.login_subheading', type: 'text', icon: <Type size={16} />, section: 'login' },
  { key: 'branding.login_background_url', type: 'url', icon: <Image size={16} />, section: 'login' },
  { key: 'branding.login_sso_button_text', type: 'text', icon: <Shield size={16} />, section: 'login' },
  { key: 'branding.login_help_text', type: 'textarea', icon: <Type size={16} />, section: 'login' },
  // Identity Protection
  { key: 'branding.powered_by_visible', type: 'toggle', icon: <Shield size={16} />, section: 'protection' },
  // Custom Navigation Links
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
  const visualFields = BRANDING_FIELDS.filter((f) => f.section === 'visual');
  const contentFields = BRANDING_FIELDS.filter((f) => f.section === 'content');
  const loginFields = BRANDING_FIELDS.filter((f) => f.section === 'login');
  const protectionFields = BRANDING_FIELDS.filter((f) => f.section === 'protection');
  const linkFields = BRANDING_FIELDS.filter((f) => f.section === 'links');

  const accentColor = values['branding.accent_color'] || '#3B82F6';
  const logoUrl = previewTheme === 'dark'
    ? (values['branding.logo_dark_url'] || values['branding.logo_url'])
    : values['branding.logo_url'];
  const loginLogoUrl = values['branding.login_logo_url'];

  return (
    <PageContainer>
      <PageHeader
        title={t('branding.admin.title')}
        subtitle={t('branding.admin.description')}
        icon={<Palette size={24} />}
      />

      {/* Identity Protection Notice */}
      <div className="mt-6 rounded-lg border border-warning/30 bg-warning/5 px-4 py-3 flex items-start gap-3">
        <Shield size={18} className="text-warning shrink-0 mt-0.5" />
        <div>
          <p className="text-sm font-medium text-heading">{t('branding.admin.identityNotice.title')}</p>
          <p className="text-xs text-muted mt-1">{t('branding.admin.identityNotice.description')}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6 mt-6">
        {/* Column 1: Identity, Visual, Content, Login, Protection, Links */}
        <div className="xl:col-span-2 space-y-6">
          {/* Identity Section */}
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

          {/* Visual Section */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Palette size={18} />
              <span>{t('branding.admin.visual.title')}</span>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                {visualFields.map((field) => (
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

          {/* Content Section */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Type size={18} />
              <span>{t('branding.admin.content.title')}</span>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                {contentFields.map((field) => (
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

          {/* Login Page Section */}
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

          {/* Identity Protection Section */}
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

          {/* Custom Navigation Links Section */}
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

        {/* Column 2: Live Preview */}
        <div className="space-y-6">
          {/* App Preview */}
          <Card>
            <CardHeader className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Eye size={18} />
                <span>{t('branding.admin.preview.title')}</span>
              </div>
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  onClick={() => setPreviewTheme('light')}
                  className={`p-1.5 rounded ${previewTheme === 'light' ? 'bg-accent text-on-accent' : 'text-muted hover:text-heading'}`}
                  aria-label={t('branding.admin.preview.light')}
                >
                  <Sun size={14} />
                </button>
                <button
                  type="button"
                  onClick={() => setPreviewTheme('dark')}
                  className={`p-1.5 rounded ${previewTheme === 'dark' ? 'bg-accent text-on-accent' : 'text-muted hover:text-heading'}`}
                  aria-label={t('branding.admin.preview.dark')}
                >
                  <Moon size={14} />
                </button>
              </div>
            </CardHeader>
            <CardBody>
              {/* App Preview Miniature */}
              <div
                className="rounded-lg border border-edge overflow-hidden"
                style={{
                  background: previewTheme === 'dark' ? '#081120' : '#F8F9FC',
                  color: previewTheme === 'dark' ? '#F2F7FF' : '#0C243C',
                }}
              >
                {/* Mini sidebar header */}
                <div
                  className="px-4 py-3 border-b flex items-center gap-2"
                  style={{
                    borderColor: previewTheme === 'dark' ? 'rgba(129,170,214,0.14)' : 'rgba(15,23,42,0.12)',
                    background: previewTheme === 'dark' ? '#0F1E38' : '#FFFFFF',
                  }}
                >
                  {logoUrl ? (
                    <img
                      src={logoUrl}
                      alt="Logo preview"
                      className="h-6 w-auto max-w-[120px] object-contain"
                      onError={(e) => {
                        (e.target as HTMLImageElement).style.display = 'none';
                      }}
                    />
                  ) : (
                    <div className="flex items-center gap-1.5">
                      <Monitor size={16} style={{ color: accentColor }} />
                      <span className="text-xs font-bold">
                        {values['instance.name'] || 'NexTraceOne'}
                      </span>
                    </div>
                  )}
                </div>

                {/* Mini content area */}
                <div className="p-4 space-y-3">
                  {/* Welcome message preview */}
                  {values['branding.welcome_message'] && (
                    <div
                      className="rounded-md px-3 py-2 text-xs"
                      style={{
                        background: `${accentColor}15`,
                        borderLeft: `3px solid ${accentColor}`,
                      }}
                    >
                      {values['branding.welcome_message']}
                    </div>
                  )}

                  {/* Accent color button preview */}
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      className="px-3 py-1.5 rounded-md text-xs text-white font-medium"
                      style={{ background: accentColor }}
                      disabled
                    >
                      {t('branding.admin.preview.primaryButton')}
                    </button>
                    <button
                      type="button"
                      className="px-3 py-1.5 rounded-md text-xs font-medium border"
                      style={{
                        borderColor: accentColor,
                        color: accentColor,
                      }}
                      disabled
                    >
                      {t('branding.admin.preview.secondaryButton')}
                    </button>
                  </div>

                  {/* Accent color swatch */}
                  <div className="flex items-center gap-2 text-[10px] opacity-70">
                    <div
                      className="w-4 h-4 rounded-full border border-edge"
                      style={{ background: accentColor }}
                    />
                    {accentColor}
                  </div>
                </div>

                {/* Mini footer preview */}
                <div
                  className="border-t px-4 py-2 flex items-center justify-between text-[10px] opacity-60"
                  style={{
                    borderColor: previewTheme === 'dark' ? 'rgba(129,170,214,0.14)' : 'rgba(15,23,42,0.12)',
                  }}
                >
                  <span className="flex-1 text-center">
                    {values['branding.footer_text'] || t('footer.default', { name: values['instance.name'] || 'NexTraceOne' })}
                  </span>
                  {(values['branding.powered_by_visible'] ?? 'true') !== 'false' && (
                    <span className="text-[9px] opacity-50 shrink-0 ml-2">Powered by NexTraceOne</span>
                  )}
                </div>
              </div>
            </CardBody>
          </Card>

          {/* Login Preview */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <LogIn size={18} />
              <span>{t('branding.admin.preview.loginTitle')}</span>
            </CardHeader>
            <CardBody>
              <div
                className="rounded-lg border border-edge overflow-hidden"
                style={{
                  background: previewTheme === 'dark' ? '#081120' : '#F8F9FC',
                  color: previewTheme === 'dark' ? '#F2F7FF' : '#0C243C',
                }}
              >
                {/* Login background area */}
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
                  {/* Login logo */}
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

                  {/* Login heading */}
                  <span className="text-xs font-semibold text-center">
                    {values['branding.login_heading'] || t('auth.welcomeTitle')}
                  </span>

                  {/* Login subheading */}
                  <span className="text-[10px] opacity-70 text-center">
                    {values['branding.login_subheading'] || t('auth.signInSubtitle')}
                  </span>

                  {/* Mini form preview */}
                  <div className="w-full space-y-2 mt-2">
                    <div
                      className="h-7 rounded-md border w-full"
                      style={{ borderColor: previewTheme === 'dark' ? 'rgba(129,170,214,0.2)' : 'rgba(15,23,42,0.15)' }}
                    />
                    <div
                      className="h-7 rounded-md border w-full"
                      style={{ borderColor: previewTheme === 'dark' ? 'rgba(129,170,214,0.2)' : 'rgba(15,23,42,0.15)' }}
                    />
                    <div
                      className="h-7 rounded-md w-full text-center text-[10px] text-white font-medium flex items-center justify-center"
                      style={{ background: accentColor }}
                    >
                      Sign In
                    </div>
                  </div>

                  {/* SSO button preview */}
                  {values['branding.login_sso_button_text'] && (
                    <div
                      className="h-7 rounded-md border w-full text-center text-[10px] flex items-center justify-center"
                      style={{ borderColor: previewTheme === 'dark' ? 'rgba(129,170,214,0.2)' : 'rgba(15,23,42,0.15)' }}
                    >
                      {values['branding.login_sso_button_text']}
                    </div>
                  )}

                  {/* Help text */}
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

      {/* Save button */}
      <div className="mt-6 flex items-center gap-3">
        <button
          className="btn btn-primary flex items-center gap-2"
          onClick={() => saveMutation.mutate()}
          disabled={saveStatus === 'saving'}
        >
          <Save size={16} />
          {saveStatus === 'saving'
            ? t('common.saving', 'Saving...')
            : t('branding.admin.save')}
        </button>
        {saveStatus === 'saved' && (
          <span className="flex items-center gap-1 text-success text-sm">
            <CheckCircle size={14} /> {t('branding.admin.saved')}
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

/** Individual field editor component for branding parameters. */
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
        <button
          type="button"
          onClick={() => onReset(field.key)}
          className="p-1 rounded text-muted hover:text-heading transition-colors"
          title={t('common.reset', 'Reset')}
          aria-label={t('common.reset', 'Reset')}
        >
          <RotateCcw size={12} />
        </button>
      </div>

      {description && (
        <p className="text-xs text-muted">{description}</p>
      )}

      {field.type === 'toggle' ? (
        <label className="relative inline-flex items-center cursor-pointer">
          <input
            type="checkbox"
            checked={value === 'true' || value === ''}
            onChange={(e) => onChange(field.key, e.target.checked ? 'true' : 'false')}
            className="sr-only peer"
          />
          <div className="w-9 h-5 bg-gray-500/30 peer-focus:ring-2 peer-focus:ring-accent rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-accent" />
        </label>
      ) : field.type === 'color' ? (
        <div className="flex items-center gap-3">
          <input
            type="color"
            value={value || '#3B82F6'}
            onChange={(e) => onChange(field.key, e.target.value)}
            className="h-9 w-14 rounded border border-edge cursor-pointer"
          />
          <input
            type="text"
            value={value}
            onChange={(e) => onChange(field.key, e.target.value)}
            placeholder="#3B82F6"
            className="flex-1 rounded-md border border-edge bg-input px-3 py-2 text-sm text-heading placeholder:text-faded focus:ring-2 focus:ring-accent focus:border-accent"
            maxLength={7}
          />
        </div>
      ) : field.type === 'textarea' ? (
        <textarea
          value={value}
          onChange={(e) => onChange(field.key, e.target.value)}
          placeholder={t(`config.${field.key}.placeholder`, { defaultValue: '' })}
          className="w-full rounded-md border border-edge bg-input px-3 py-2 text-sm text-heading placeholder:text-faded focus:ring-2 focus:ring-accent focus:border-accent resize-y"
          rows={3}
          maxLength={500}
        />
      ) : (
        <input
          type={field.type === 'url' ? 'url' : 'text'}
          value={value}
          onChange={(e) => onChange(field.key, e.target.value)}
          placeholder={t(`config.${field.key}.placeholder`, { defaultValue: '' })}
          className="w-full rounded-md border border-edge bg-input px-3 py-2 text-sm text-heading placeholder:text-faded focus:ring-2 focus:ring-accent focus:border-accent"
        />
      )}
    </div>
  );
}

export default BrandingAdminPage;
