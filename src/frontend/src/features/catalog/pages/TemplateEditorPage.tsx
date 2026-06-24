import { useState, useMemo, type FormEvent } from 'react';
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
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, TextArea, Select } from '../../../shared/ui';

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

  // Carrega template existente ao editar
  const { data: existing, isError: isLoadError } = useQuery({
    queryKey: ['service-template', id],
    queryFn: () => templatesApi.getById(id!),
    enabled: isEditing,
  });

  // Deriva estado inicial a partir dos dados existentes (evita setState-in-useEffect)
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

  // Estado do formulário — objeto único, reinicializado quando os dados chegam
  const [form, setForm] = useState<TemplateFormState>(DEFAULT_FORM_STATE);
  const [formKey, setFormKey] = useState(0);
  const [error, setError] = useState<string | null>(null);

  // Sincroniza estado quando os dados iniciais chegam (reset por key evita setState-in-effect)
  const currentKey = existing ? existing.slug : '__new__';
  if (currentKey !== '__new__' && formKey === 0) {
    setForm(initialState);
    setFormKey(1);
  }

  const updateField = <K extends keyof TemplateFormState>(key: K, value: TemplateFormState[K]) => {
    setForm(prev => ({ ...prev, [key]: value }));
  };

  // Desestrutura para acesso mais fácil no JSX
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

  const handleSubmit = (e: FormEvent) => {
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

  // Arrays de opções (construídos no render para t() estar disponível)
  const serviceTypeOptions = [
    { value: 'RestApi', label: t('templates.editor.serviceTypes.restApi', 'REST API') },
    { value: 'EventDriven', label: t('templates.editor.serviceTypes.eventDriven', 'Event Driven') },
    { value: 'BackgroundWorker', label: t('templates.editor.serviceTypes.backgroundWorker', 'Background Worker') },
    { value: 'Grpc', label: t('templates.editor.serviceTypes.grpc', 'gRPC') },
    { value: 'Soap', label: t('templates.editor.serviceTypes.soap', 'SOAP') },
    { value: 'Generic', label: t('templates.editor.serviceTypes.generic', 'Generic') },
  ];

  const languageOptions = [
    { value: 'DotNet', label: t('templates.editor.languages.dotnet', '.NET') },
    { value: 'NodeJs', label: t('templates.editor.languages.nodejs', 'Node.js') },
    { value: 'Java', label: t('templates.editor.languages.java', 'Java') },
    { value: 'Go', label: t('templates.editor.languages.go', 'Go') },
    { value: 'Python', label: t('templates.editor.languages.python', 'Python') },
    { value: 'Agnostic', label: t('templates.editor.languages.agnostic', 'Agnostic') },
  ];

  if (isLoadError) {
    return (
      <div className="flex flex-col gap-5 p-6">
        <PageErrorState />
      </div>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={isEditing ? t('templates.editor.editTitle') : t('templates.editor.createTitle')}
        subtitle={t('catalog.templateEditor.subtitle', 'Define template metadata, classification, and scaffolding manifest')}
      />
      {/* Navegação de volta */}
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="sm"
          icon={<ArrowLeft className="h-4 w-4" />}
          onClick={() => navigate(isEditing ? `/catalog/templates/${id}` : '/catalog/templates')}
        >
          {t('common.back')}
        </Button>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded border border-critical/30 bg-critical/10 p-3 text-sm text-critical">
          <AlertCircle className="h-4 w-4 shrink-0" />
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="flex flex-col gap-5">
        {/* Secção de identidade */}
        <div className="flex flex-col gap-4 rounded-lg border border-edge bg-elevated p-5">
          <h2 className="text-sm font-medium text-body">{t('templates.editor.sections.identity')}</h2>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {!isEditing && (
              <TextField
                label={t('templates.editor.fields.slug')}
                placeholder={t('catalog.template.placeholder.slug', 'payment-api')}
                value={slug}
                onChange={e => updateField('slug', e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
                required
                helperText={t('templates.editor.hints.slug')}
                size="sm"
              />
            )}

            <TextField
              label={t('templates.editor.fields.displayName')}
              placeholder={t('templates.editor.placeholders.displayName')}
              value={displayName}
              onChange={e => updateField('displayName', e.target.value)}
              required
              size="sm"
            />

            <TextField
              label={t('templates.editor.fields.version')}
              placeholder={t('catalog.template.placeholder.version', '1.0.0')}
              value={version}
              onChange={e => updateField('version', e.target.value)}
              required
              size="sm"
            />
          </div>

          <TextArea
            label={t('templates.editor.fields.description')}
            placeholder={t('templates.editor.placeholders.description')}
            value={description}
            onChange={e => updateField('description', e.target.value)}
            required
            rows={3}
            textareaClassName="resize-none"
          />
        </div>

        {/* Secção de classificação */}
        <div className="flex flex-col gap-4 rounded-lg border border-edge bg-elevated p-5">
          <h2 className="text-sm font-medium text-body">{t('templates.editor.sections.classification')}</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Select
              label={t('templates.editor.fields.serviceType')}
              options={serviceTypeOptions}
              value={serviceType}
              onChange={e => updateField('serviceType', e.target.value as TemplateServiceType)}
              disabled={isEditing}
              size="sm"
            />

            <Select
              label={t('templates.editor.fields.language')}
              options={languageOptions}
              value={language}
              onChange={e => updateField('language', e.target.value as TemplateLanguage)}
              disabled={isEditing}
              size="sm"
            />
          </div>

          {isEditing && (
            <p className="text-xs text-muted">{t('templates.editor.hints.typeLanguageImmutable')}</p>
          )}
        </div>

        {/* Secção de ownership */}
        <div className="flex flex-col gap-4 rounded-lg border border-edge bg-elevated p-5">
          <h2 className="text-sm font-medium text-body">{t('templates.editor.sections.ownership')}</h2>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <TextField
              label={t('templates.editor.fields.defaultDomain')}
              placeholder={t('templates.editor.placeholders.domain')}
              value={defaultDomain}
              onChange={e => updateField('defaultDomain', e.target.value)}
              required
              size="sm"
            />

            <TextField
              label={t('templates.editor.fields.defaultTeam')}
              placeholder={t('templates.editor.placeholders.team')}
              value={defaultTeam}
              onChange={e => updateField('defaultTeam', e.target.value)}
              required
              size="sm"
            />
          </div>

          <TextField
            label={t('templates.editor.fields.tags')}
            placeholder={t('catalog.template.placeholder.tags', 'ddd, clean-architecture, payments')}
            value={tagsInput}
            onChange={e => updateField('tagsInput', e.target.value)}
            helperText={t('templates.editor.hints.tags')}
            size="sm"
          />
        </div>

        {/* Secção avançada */}
        <div className="flex flex-col gap-4 rounded-lg border border-edge bg-elevated p-5">
          <h2 className="text-sm font-medium text-body">{t('templates.editor.sections.advanced')}</h2>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <TextField
              label={t('templates.editor.fields.repoUrl')}
              placeholder={t('catalog.template.placeholder.repoUrl', 'https://github.com/org/template-name')}
              value={repositoryTemplateUrl}
              onChange={e => updateField('repositoryTemplateUrl', e.target.value)}
              helperText={t('templates.editor.hints.repoUrl')}
              size="sm"
            />

            <TextField
              label={t('templates.editor.fields.repoBranch')}
              placeholder={t('catalog.template.placeholder.repoBranch', 'main')}
              value={repositoryTemplateBranch}
              onChange={e => updateField('repositoryTemplateBranch', e.target.value)}
              size="sm"
            />
          </div>

          <TextArea
            label={t('templates.editor.fields.baseContractSpec')}
            placeholder={t('catalog.template.placeholder.baseContractSpec', 'openapi: 3.0.0&#10;info:&#10;  title: Payment API&#10;  version: 1.0.0&#10;paths: ...')}
            value={baseContractSpec}
            onChange={e => updateField('baseContractSpec', e.target.value)}
            rows={8}
            textareaClassName="resize-none font-mono text-xs"
            helperText={t('templates.editor.hints.baseContractSpec')}
          />

          <TextArea
            label={t('templates.editor.fields.scaffoldingManifest')}
            placeholder={'[\n  { "path": "src/Controllers/{{ServiceNamePascal}}Controller.cs", "content": "..." },\n  { "path": "README.md", "content": "# {{ServiceName}}" }\n]'}
            value={scaffoldingManifestJson}
            onChange={e => updateField('scaffoldingManifestJson', e.target.value)}
            rows={8}
            textareaClassName="resize-none font-mono text-xs"
            helperText={t('templates.editor.hints.scaffoldingManifest')}
          />
        </div>

        {/* Submissão */}
        <div className="flex justify-end gap-3">
          <Button
            variant="outline"
            type="button"
            onClick={() => navigate(isEditing ? `/catalog/templates/${id}` : '/catalog/templates')}
          >
            {t('common.cancel')}
          </Button>
          <Button
            type="submit"
            variant="primary"
            loading={isPending}
            icon={<Save className="h-4 w-4" />}
          >
            {isPending ? t('common.saving') : t('common.save')}
          </Button>
        </div>
      </form>
    </PageContainer>
  );
}
