import { useState, useEffect } from 'react';
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

// ── Main page ─────────────────────────────────────────────────────────────────

export function TemplateEditorPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isEditing = !!id;

  // Form state
  const [slug, setSlug] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [description, setDescription] = useState('');
  const [version, setVersion] = useState('1.0.0');
  const [serviceType, setServiceType] = useState<TemplateServiceType>('RestApi');
  const [language, setLanguage] = useState<TemplateLanguage>('DotNet');
  const [defaultDomain, setDefaultDomain] = useState('');
  const [defaultTeam, setDefaultTeam] = useState('');
  const [tagsInput, setTagsInput] = useState('');
  const [baseContractSpec, setBaseContractSpec] = useState('');
  const [scaffoldingManifestJson, setScaffoldingManifestJson] = useState('');
  const [repositoryTemplateUrl, setRepositoryTemplateUrl] = useState('');
  const [repositoryTemplateBranch, setRepositoryTemplateBranch] = useState('');
  const [error, setError] = useState<string | null>(null);

  // Load existing template when editing
  const { data: existing } = useQuery({
    queryKey: ['service-template', id],
    queryFn: () => templatesApi.getById(id!),
    enabled: isEditing,
  });

  useEffect(() => {
    if (existing) {
      setSlug(existing.slug);
      setDisplayName(existing.displayName);
      setDescription(existing.description);
      setVersion(existing.version);
      setServiceType(existing.serviceType);
      setLanguage(existing.language);
      setDefaultDomain(existing.defaultDomain);
      setDefaultTeam(existing.defaultTeam);
      setTagsInput(existing.tags.join(', '));
      setBaseContractSpec(existing.baseContractSpec ?? '');
      setScaffoldingManifestJson(existing.scaffoldingManifestJson ?? '');
      setRepositoryTemplateUrl(existing.repositoryTemplateUrl ?? '');
      setRepositoryTemplateBranch(existing.repositoryTemplateBranch ?? '');
    }
  }, [existing]);

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
                  onChange={e => setSlug(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
                  required
                />
              </FormField>
            )}

            <FormField label={t('templates.editor.fields.displayName')} required>
              <input
                className={INPUT_CLASS}
                placeholder={t('templates.editor.placeholders.displayName')}
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                required
              />
            </FormField>

            <FormField label={t('templates.editor.fields.version')} required>
              <input
                className={INPUT_CLASS}
                placeholder="1.0.0"
                value={version}
                onChange={e => setVersion(e.target.value)}
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
              onChange={e => setDescription(e.target.value)}
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
                onChange={e => setServiceType(e.target.value as TemplateServiceType)}
                disabled={isEditing}
              >
                <option value="RestApi">REST API</option>
                <option value="EventDriven">Event Driven</option>
                <option value="BackgroundWorker">Background Worker</option>
                <option value="Grpc">gRPC</option>
                <option value="Soap">SOAP</option>
                <option value="Generic">Generic</option>
              </select>
            </FormField>

            <FormField label={t('templates.editor.fields.language')} required>
              <select
                className={SELECT_CLASS}
                value={language}
                onChange={e => setLanguage(e.target.value as TemplateLanguage)}
                disabled={isEditing}
              >
                <option value="DotNet">.NET</option>
                <option value="NodeJs">Node.js</option>
                <option value="Java">Java</option>
                <option value="Go">Go</option>
                <option value="Python">Python</option>
                <option value="Agnostic">Agnostic</option>
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
                onChange={e => setDefaultDomain(e.target.value)}
                required
              />
            </FormField>

            <FormField label={t('templates.editor.fields.defaultTeam')} required>
              <input
                className={INPUT_CLASS}
                placeholder={t('templates.editor.placeholders.team')}
                value={defaultTeam}
                onChange={e => setDefaultTeam(e.target.value)}
                required
              />
            </FormField>
          </div>

          <FormField label={t('templates.editor.fields.tags')} hint={t('templates.editor.hints.tags')}>
            <input
              className={INPUT_CLASS}
              placeholder="ddd, clean-architecture, payments"
              value={tagsInput}
              onChange={e => setTagsInput(e.target.value)}
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
                onChange={e => setRepositoryTemplateUrl(e.target.value)}
              />
            </FormField>

            <FormField label={t('templates.editor.fields.repoBranch')}>
              <input
                className={INPUT_CLASS}
                placeholder="main"
                value={repositoryTemplateBranch}
                onChange={e => setRepositoryTemplateBranch(e.target.value)}
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
              onChange={e => setBaseContractSpec(e.target.value)}
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
              onChange={e => setScaffoldingManifestJson(e.target.value)}
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
