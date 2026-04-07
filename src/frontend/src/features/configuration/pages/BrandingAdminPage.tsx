import { useTranslation } from 'react-i18next';
import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Palette, Image, Type, Globe, Save, CheckCircle, XCircle,
  RotateCcw, Eye, Monitor, Moon, Sun,
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
 * - Instance name e display settings
 *
 * Pilar: Platform Customization + Source of Truth
 * Persona: Platform Admin
 */

interface BrandingField {
  key: string;
  type: 'text' | 'color' | 'url' | 'textarea';
  icon: React.ReactNode;
  section: 'identity' | 'visual' | 'content';
}

const BRANDING_FIELDS: BrandingField[] = [
  { key: 'instance.name', type: 'text', icon: <Globe size={16} />, section: 'identity' },
  { key: 'instance.commercial_name', type: 'text', icon: <Globe size={16} />, section: 'identity' },
  { key: 'branding.logo_url', type: 'url', icon: <Image size={16} />, section: 'visual' },
  { key: 'branding.logo_dark_url', type: 'url', icon: <Image size={16} />, section: 'visual' },
  { key: 'branding.accent_color', type: 'color', icon: <Palette size={16} />, section: 'visual' },
  { key: 'branding.favicon_url', type: 'url', icon: <Image size={16} />, section: 'visual' },
  { key: 'branding.welcome_message', type: 'textarea', icon: <Type size={16} />, section: 'content' },
  { key: 'branding.footer_text', type: 'textarea', icon: <Type size={16} />, section: 'content' },
];

const BRANDING_KEYS = BRANDING_FIELDS.map((f) => f.key);

async function fetchBrandingSettings(): Promise<EffectiveConfigurationDto[]> {
  const results: EffectiveConfigurationDto[] = [];
  for (const key of BRANDING_KEYS) {
    try {
      const result = await configurationApi.getEffectiveSettings('System', null, key);
      results.push(...result);
    } catch {
      // Key may not exist yet
    }
  }
  return results;
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
      for (const key of BRANDING_KEYS) {
        const value = values[key];
        if (value !== undefined) {
          await configurationApi.setConfigurationValue(key, {
            value,
            scope: 'System',
            changeReason: 'Branding update via admin page',
          });
        }
      }
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

  const accentColor = values['branding.accent_color'] || '#3B82F6';
  const logoUrl = previewTheme === 'dark'
    ? (values['branding.logo_dark_url'] || values['branding.logo_url'])
    : values['branding.logo_url'];

  return (
    <PageContainer>
      <PageHeader
        title={t('branding.admin.title')}
        subtitle={t('branding.admin.description')}
        icon={<Palette size={24} />}
      />

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6 mt-6">
        {/* Column 1: Identity & Visual */}
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
        </div>

        {/* Column 2: Live Preview */}
        <div className="space-y-6">
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
              {/* Preview Miniature */}
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
                {values['branding.footer_text'] && (
                  <div
                    className="border-t px-4 py-2 text-center text-[10px] opacity-60"
                    style={{
                      borderColor: previewTheme === 'dark' ? 'rgba(129,170,214,0.14)' : 'rgba(15,23,42,0.12)',
                    }}
                  >
                    {values['branding.footer_text']}
                  </div>
                )}
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

      {field.type === 'color' ? (
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
