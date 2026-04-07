import { useState, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Save, AlertCircle } from 'lucide-react';
import {
  templatesApi,
  type TemplateLanguage,
  type TemplateServiceType,
  type CreateTemplateRequest,
  type UpdateTemplateRequest,
} from '../api/templates';
import { PageErrorState } from '../../../components/PageErrorState';

// ── Form field ────────────────────────────────────────────────────────────────

function FormField({
  label,
  required,
  hint,
  children,
}: {
  label: string;
  required?: boolean;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-xs font-medium text-neutral-300">
        {label}
        {required && <span className="ml-0.5 text-red-400">*</span>}
      </label>
      {children}
      {hint && <p className="text-xs text-neutral-500">{hint}</p>}
    </div>
  );
}

const INPUT_CLASS =
  'w-full rounded border border-neutral-700 bg-neutral-800 px-3 py-2 text-sm text-neutral-200 placeholder-neutral-500 outline-none focus:border-blue-500';

const SELECT_CLASS =
  'w-full rounded border border-neutral-700 bg-neutral-800 px-3 py-2 text-sm text-neutral-200 outline-none focus:border-blue-500';

// ── Form state ────────────────────────────────────────────────────────────────

interface TemplateFormState {
  slug: string;
  displayName: string;
  description: string;
  version: string;
  serviceType: TemplateServiceType;
  language: TemplateLanguage;
  defaultDomain: string;
  defaultTeam: string;
  tagsInput: string;
  baseContractSpec: string;
  scaffoldingManifestJson: string;
  repositoryTemplateUrl: string;
  repositoryTemplateBranch: string;
}

const DEFAULT_FORM_STATE: TemplateFormState = {
  slug: '',
  displayName: '',
  description: '',
  version: '1.0.0',
  serviceType: 'RestApi',
  language: 'DotNet',
  defaultDomain: '',
  defaultTeam: '',
  tagsInput: '',
  baseContractSpec: '',
  scaffoldingManifestJson: '',
  repositoryTemplateUrl: '',
  repositoryTemplateBranch: '',
};

// ── Main page ─────────────────────────────────────────────────────────────────

export function TemplateEditorPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEditing = !!id;

  // Load existing template when editing
  const { data: existing, isError: isLoadError } = useQuery({
    queryKey: ['service-template', id],
    queryFn: () => templatesApi.getById(id!),
    enabled: isEditing,
  });

  // Derive initial form state from existing data (avoids setState-in-useEffect)
  const initialState = useMemo<TemplateFormState>(() => {
    if (!existing) return DEFAULT_FORM_STATE;
    return {
      slug: existing.slug,
      displayName: existing.displayName,
      description: existing.description,
      version: existing.version,
      serviceType: existing.serviceType,
      language: existing.language,
      defaultDomain: existing.defaultDomain,
      defaultTeam: existing.defaultTeam,
      tagsInput: existing.tags.join(', '),
      baseContractSpec: existing.baseContractSpec ?? '',
      scaffoldingManifestJson: existing.scaffoldingManifestJson ?? '',
      repositoryTemplateUrl: existing.repositoryTemplateUrl ?? '',
      repositoryTemplateBranch: existing.repositoryTemplateBranch ?? '',
    };
  }, [existing]);

  // Form state — single object, re-initialized when existing data loads
  const [form, setForm] = useState<TemplateFormState>(DEFAULT_FORM_STATE);
  const [formKey, setFormKey] = useState(0);
  const [error, setError] = useState<string | null>(null);

  // Sync form state when initial data arrives (key-based reset avoids setState-in-effect)
  const currentKey = existing ? existing.slug : '__new__';
  if (currentKey !== '__new__' && formKey === 0) {
    setForm(initialState);
    setFormKey(1);
  }

  const updateField = <K extends keyof TemplateFormState>(key: K, value: TemplateFormState[K]) => {
    setForm(prev => ({ ...prev, [key]: value }));
  };

  // Destructure for easier access in JSX
  const {
    slug, displayName, description, version, serviceType, language,
    defaultDomain, defaultTeam, tagsInput, baseContractSpec,
    scaffoldingManifestJson, repositoryTemplateUrl, repositoryTemplateBranch,
  } = form;

  const createMutation = useMutation({
    mutationFn: (body: CreateTemplateRequest) => templatesApi.create(body),
    onSuccess: result => {
      queryClient.invalidateQueries({ queryKey: ['service-templates'] });
      navigate(`/catalog/templates/${result.templateId}`);
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : t('templates.editor.saveError');
      setError(msg);
    },
  });

  const updateMutation = useMutation({
    mutationFn: (body: UpdateTemplateRequest) => templatesApi.update(id!, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['service-template', id] });
      queryClient.invalidateQueries({ queryKey: ['service-templates'] });
      navigate(`/catalog/templates/${id}`);
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : t('templates.editor.saveError');
      setError(msg);
    },
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const parseTags = () =>
    tagsInput
      .split(',')
      .map(s => s.trim())
      .filter(Boolean);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (isEditing) {
      updateMutation.mutate({
        displayName,
        description,
        version,
        defaultDomain,
        defaultTeam,
        tags: parseTags(),
        baseContractSpec: baseContractSpec || undefined,
        scaffoldingManifestJson: scaffoldingManifestJson || undefined,
        repositoryTemplateUrl: repositoryTemplateUrl || undefined,
        repositoryTemplateBranch: repositoryTemplateBranch || undefined,
      });
    } else {
      createMutation.mutate({
        slug,
        displayName,
        description,
        version,
        serviceType,
        language,
        defaultDomain,
        defaultTeam,
        tags: parseTags(),
        baseContractSpec: baseContractSpec || undefined,
        scaffoldingManifestJson: scaffoldingManifestJson || undefined,
        repositoryTemplateUrl: repositoryTemplateUrl || undefined,
        repositoryTemplateBranch: repositoryTemplateBranch || undefined,
      });
    }
  };

  if (isLoadError) {
    return (
      <div className="flex flex-col gap-5 p-6">
        <PageErrorState />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5 p-6">
      {/* Header */}
      <div className="flex items-center justify-between gap-4">
        <button
          onClick={() => navigate(isEditing ? `/catalog/templates/${id}` : '/catalog/templates')}
          className="flex items-center gap-1.5 text-sm text-neutral-400 hover:text-neutral-200"
        >
          <ArrowLeft className="h-4 w-4" />
          {t('common.back')}
        </button>
        <h1 className="text-lg font-semibold text-neutral-100">
          {isEditing ? t('templates.editor.editTitle') : t('templates.editor.createTitle')}
        </h1>
        <div />
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-400">
          <AlertCircle className="h-4 w-4 shrink-0" />
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="flex flex-col gap-5">
        {/* Identity section */}
        <div className="flex flex-col gap-4 rounded-lg border border-neutral-800 bg-neutral-900 p-5">
          <h2 className="text-sm font-medium text-neutral-300">{t('templates.editor.sections.identity')}</h2>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {!isEditing && (
              <FormField
                label={t('templates.editor.fields.slug')}
                required
                hint={t('templates.editor.hints.slug')}
              >
                <input
                  className={INPUT_CLASS}
                  placeholder="payment-api"
                  value={slug}
                  onChange={e => updateField('slug', e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
                  required
                />
              </FormField>
            )}

            <FormField label={t('templates.editor.fields.displayName')} required>
              <input
                className={INPUT_CLASS}
                placeholder={t('templates.editor.placeholders.displayName')}
                value={displayName}
                onChange={e => updateField('displayName', e.target.value)}
                required
              />
            </FormField>

            <FormField label={t('templates.editor.fields.version')} required>
              <input
                className={INPUT_CLASS}
                placeholder="1.0.0"
                value={version}
                onChange={e => updateField('version', e.target.value)}
                required
              />
            </FormField>
          </div>

          <FormField label={t('templates.editor.fields.description')} required>
            <textarea
              className={`${INPUT_CLASS} resize-none`}
              rows={3}
              placeholder={t('templates.editor.placeholders.description')}
              value={description}
              onChange={e => updateField('description', e.target.value)}
              required
            />
          </FormField>
        </div>

        {/* Classification section */}
        <div className="flex flex-col gap-4 rounded-lg border border-neutral-800 bg-neutral-900 p-5">
          <h2 className="text-sm font-medium text-neutral-300">{t('templates.editor.sections.classification')}</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <FormField label={t('templates.editor.fields.serviceType')} required>
              <select
                className={SELECT_CLASS}
                value={serviceType}
                onChange={e => updateField('serviceType', e.target.value as TemplateServiceType)}
                disabled={isEditing}
              >
                <option value="RestApi">{t('templates.editor.serviceTypes.restApi', 'REST API')}</option>
                <option value="EventDriven">{t('templates.editor.serviceTypes.eventDriven', 'Event Driven')}</option>
                <option value="BackgroundWorker">{t('templates.editor.serviceTypes.backgroundWorker', 'Background Worker')}</option>
                <option value="Grpc">{t('templates.editor.serviceTypes.grpc', 'gRPC')}</option>
                <option value="Soap">{t('templates.editor.serviceTypes.soap', 'SOAP')}</option>
                <option value="Generic">{t('templates.editor.serviceTypes.generic', 'Generic')}</option>
              </select>
            </FormField>

            <FormField label={t('templates.editor.fields.language')} required>
              <select
                className={SELECT_CLASS}
                value={language}
                onChange={e => updateField('language', e.target.value as TemplateLanguage)}
                disabled={isEditing}
              >
                <option value="DotNet">{t('templates.editor.languages.dotnet', '.NET')}</option>
                <option value="NodeJs">{t('templates.editor.languages.nodejs', 'Node.js')}</option>
                <option value="Java">{t('templates.editor.languages.java', 'Java')}</option>
                <option value="Go">{t('templates.editor.languages.go', 'Go')}</option>
                <option value="Python">{t('templates.editor.languages.python', 'Python')}</option>
                <option value="Agnostic">{t('templates.editor.languages.agnostic', 'Agnostic')}</option>
              </select>
            </FormField>
          </div>

          {isEditing && (
            <p className="text-xs text-neutral-500">{t('templates.editor.hints.typeLanguageImmutable')}</p>
          )}
        </div>

        {/* Ownership section */}
        <div className="flex flex-col gap-4 rounded-lg border border-neutral-800 bg-neutral-900 p-5">
          <h2 className="text-sm font-medium text-neutral-300">{t('templates.editor.sections.ownership')}</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <FormField label={t('templates.editor.fields.defaultDomain')} required>
              <input
                className={INPUT_CLASS}
                placeholder={t('templates.editor.placeholders.domain')}
                value={defaultDomain}
                onChange={e => updateField('defaultDomain', e.target.value)}
                required
              />
            </FormField>

            <FormField label={t('templates.editor.fields.defaultTeam')} required>
              <input
                className={INPUT_CLASS}
                placeholder={t('templates.editor.placeholders.team')}
                value={defaultTeam}
                onChange={e => updateField('defaultTeam', e.target.value)}
                required
              />
            </FormField>
          </div>

          <FormField label={t('templates.editor.fields.tags')} hint={t('templates.editor.hints.tags')}>
            <input
              className={INPUT_CLASS}
              placeholder="ddd, clean-architecture, payments"
              value={tagsInput}
              onChange={e => updateField('tagsInput', e.target.value)}
            />
          </FormField>
        </div>

        {/* Advanced section */}
        <div className="flex flex-col gap-4 rounded-lg border border-neutral-800 bg-neutral-900 p-5">
          <h2 className="text-sm font-medium text-neutral-300">{t('templates.editor.sections.advanced')}</h2>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <FormField label={t('templates.editor.fields.repoUrl')} hint={t('templates.editor.hints.repoUrl')}>
              <input
                className={INPUT_CLASS}
                placeholder="https://github.com/org/template-name"
                value={repositoryTemplateUrl}
                onChange={e => updateField('repositoryTemplateUrl', e.target.value)}
              />
            </FormField>

            <FormField label={t('templates.editor.fields.repoBranch')}>
              <input
                className={INPUT_CLASS}
                placeholder="main"
                value={repositoryTemplateBranch}
                onChange={e => updateField('repositoryTemplateBranch', e.target.value)}
              />
            </FormField>
          </div>

          <FormField
            label={t('templates.editor.fields.baseContractSpec')}
            hint={t('templates.editor.hints.baseContractSpec')}
          >
            <textarea
              className={`${INPUT_CLASS} resize-none font-mono text-xs`}
              rows={8}
              placeholder="openapi: 3.0.0&#10;info:&#10;  title: Payment API&#10;  version: 1.0.0&#10;paths: ..."
              value={baseContractSpec}
              onChange={e => updateField('baseContractSpec', e.target.value)}
            />
          </FormField>

          <FormField
            label={t('templates.editor.fields.scaffoldingManifest')}
            hint={t('templates.editor.hints.scaffoldingManifest')}
          >
            <textarea
              className={`${INPUT_CLASS} resize-none font-mono text-xs`}
              rows={8}
              placeholder={'[\n  { "path": "src/Controllers/{{ServiceNamePascal}}Controller.cs", "content": "..." },\n  { "path": "README.md", "content": "# {{ServiceName}}" }\n]'}
              value={scaffoldingManifestJson}
              onChange={e => updateField('scaffoldingManifestJson', e.target.value)}
            />
          </FormField>
        </div>

        {/* Submit */}
        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={() => navigate(isEditing ? `/catalog/templates/${id}` : '/catalog/templates')}
            className="rounded border border-neutral-700 bg-neutral-800 px-4 py-2 text-sm font-medium text-neutral-300 hover:bg-neutral-700"
          >
            {t('common.cancel')}
          </button>
          <button
            type="submit"
            disabled={isPending}
            className="flex items-center gap-2 rounded bg-blue-600 px-5 py-2 text-sm font-medium text-white hover:bg-blue-500 disabled:opacity-50"
          >
            <Save className="h-4 w-4" />
            {isPending ? t('common.saving') : t('common.save')}
          </button>
        </div>
      </form>
    </div>
  );
}
