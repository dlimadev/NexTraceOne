import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  ArrowRight,
  Zap,
  FileCode,
  Download,
  CheckCircle2,
  AlertCircle,
  Loader2,
  ChevronRight,
  Copy,
  BookOpen,
  Layers,
  Server,
} from 'lucide-react';
import JSZip from 'jszip';
import { templatesApi, type AiScaffoldResult, type ScaffoldedFile } from '../api/templates';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, TextArea, Select } from '../../../shared/ui';

// ── Step indicator ─────────────────────────────────────────────────────────────

const STEPS = ['template', 'intent', 'generate', 'review'] as const;
type Step = (typeof STEPS)[number];

function StepDot({ step, current }: { step: string; current: Step }) {
  const idx = STEPS.indexOf(current as Step);
  const stepIdx = STEPS.indexOf(step as Step);
  const done = stepIdx < idx;
  const active = step === current;

  return (
    <div
      className={`flex h-7 w-7 items-center justify-center rounded-full text-xs font-semibold transition-colors ${
        done
          ? 'bg-success text-on-accent'
          : active
          ? 'bg-accent text-on-accent ring-2 ring-accent/30'
          : 'bg-elevated text-muted'
      }`}
    >
      {done ? <CheckCircle2 className="h-4 w-4" /> : stepIdx + 1}
    </div>
  );
}

function StepLine({ done }: { done: boolean }) {
  return (
    <div
      className={`h-px flex-1 transition-colors ${done ? 'bg-success' : 'bg-elevated'}`}
    />
  );
}

// ── File tree ─────────────────────────────────────────────────────────────────

function FileTree({
  files,
  selected,
  onSelect,
}: {
  files: ScaffoldedFile[];
  selected: number;
  onSelect: (i: number) => void;
}) {
  return (
    <div className="flex flex-col gap-0.5 overflow-auto">
      {files.map((f, i) => (
        <Button
          key={i}
          variant="ghost"
          size="xs"
          onClick={() => onSelect(i)}
          icon={<FileCode className="h-3 w-3 shrink-0" />}
          className={`w-full justify-start truncate text-left ${
            i === selected ? 'bg-accent/20 text-accent' : ''
          }`}
        >
          <span className="truncate">{f.path}</span>
        </Button>
      ))}
    </div>
  );
}

// ── Main wizard ───────────────────────────────────────────────────────────────

export function AiScaffoldWizardPage() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  // Step state
  const [step, setStep] = useState<Step>('template');

  // Step 2 — intent
  const [serviceName, setServiceName] = useState('');
  const [serviceDescription, setServiceDescription] = useState('');
  const [teamName, setTeamName] = useState('');
  const [domain, setDomain] = useState('');
  const [mainEntities, setMainEntities] = useState('');
  const [additionalRequirements, setAdditionalRequirements] = useState('');
  const [languageOverride, setLanguageOverride] = useState('');

  // Step 4 — review
  const [selectedFile, setSelectedFile] = useState(0);
  const [copied, setCopied] = useState(false);

  const { data: template, isLoading: templateLoading } = useQuery({
    queryKey: ['service-template', id],
    queryFn: () => templatesApi.getById(id!),
    enabled: !!id,
  });

  const generateMutation = useMutation({
    mutationFn: (req: Parameters<typeof templatesApi.generateWithAi>[0]) =>
      templatesApi.generateWithAi(req),
    onSuccess: () => {
      setStep('review');
      setSelectedFile(0);
    },
  });

  const result: AiScaffoldResult | undefined = generateMutation.data;

  const handleGenerate = () => {
    setStep('generate');
    generateMutation.mutate({
      templateId: id,
      serviceName,
      serviceDescription,
      teamName: teamName || undefined,
      domain: domain || undefined,
      languageOverride: languageOverride || undefined,
      mainEntities: mainEntities || undefined,
      additionalRequirements: additionalRequirements || undefined,
    });
  };

  const handleDownloadZip = async () => {
    if (!result) return;
    const zip = new JSZip();
    result.files.forEach(f => {
      if (f.content) zip.file(f.path, f.content);
    });
    const blob = await zip.generateAsync({ type: 'blob' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${result.serviceName}-scaffold.zip`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleCopyFile = () => {
    const content = result?.files[selectedFile]?.content ?? '';
    navigator.clipboard.writeText(content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (templateLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Loader2 className="h-8 w-8 animate-spin text-muted" />
      </div>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('catalog.aiScaffold.title', 'AI Scaffold Wizard')}
        subtitle={t('catalog.aiScaffold.subtitle', 'Generate service scaffolding with AI from a template')}
      />
      {/* Back + wizard title */}
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="sm"
          icon={<ArrowLeft className="h-4 w-4" />}
          onClick={() => navigate(`/catalog/templates/${id}`)}
        >
          {t('templates.scaffold.backToTemplate')}
        </Button>
        <div className="flex items-center gap-2">
          <Zap className="h-5 w-5 text-accent" />
          <h2 className="text-lg font-semibold text-body">{t('templates.scaffold.title')}</h2>
        </div>
      </div>

      {/* Step indicator */}
      <div className="flex items-center gap-2 rounded-lg border border-edge bg-elevated p-4">
        {STEPS.map((s, i) => (
          <div key={s} className="flex flex-1 items-center gap-2">
            <StepDot step={s} current={step} />
            <span
              className={`hidden text-xs font-medium sm:block ${
                s === step ? 'text-body' : 'text-muted'
              }`}
            >
              {t(`templates.scaffold.steps.${s}`)}
            </span>
            {i < STEPS.length - 1 && (
              <StepLine done={STEPS.indexOf(step) > i} />
            )}
          </div>
        ))}
      </div>

      {/* ── Step 1: Template overview ───────────────────────────────────── */}
      {step === 'template' && template && (
        <div className="flex flex-col gap-4">
          <div className="rounded-lg border border-edge bg-elevated p-5">
            <div className="mb-3 flex items-start justify-between">
              <div>
                <h2 className="text-sm font-semibold text-body">{template.displayName}</h2>
                <p className="text-xs text-muted">{template.slug} · v{template.version}</p>
              </div>
              <div className="flex gap-1.5">
                {/* purple/orange taxonomy — no DS token yet */}
                <span className="rounded border border-purple-500/20 bg-purple-500/10 px-1.5 py-0.5 text-xs text-purple-400">
                  {template.language}
                </span>
                <span className="rounded border border-accent/20 bg-accent/10 px-1.5 py-0.5 text-xs text-accent">
                  {template.serviceType}
                </span>
              </div>
            </div>
            <p className="text-sm text-muted">{template.description}</p>
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            {[
              {
                icon: BookOpen,
                label: t('templates.scaffold.info.baseContract'),
                value: template.hasBaseContract
                  ? t('templates.scaffold.info.included')
                  : t('templates.scaffold.info.notIncluded'),
                ok: template.hasBaseContract,
              },
              {
                icon: Layers,
                label: t('templates.scaffold.info.scaffoldManifest'),
                value: template.hasScaffoldingManifest
                  ? t('templates.scaffold.info.included')
                  : t('templates.scaffold.info.aiGeneratesStructure'),
                ok: template.hasScaffoldingManifest,
              },
              {
                icon: Server,
                label: t('templates.scaffold.info.defaultDomain'),
                value: template.defaultDomain,
                ok: true,
              },
            ].map(item => (
              <div
                key={item.label}
                className="flex items-start gap-3 rounded-lg border border-edge bg-elevated p-3"
              >
                <item.icon className={`mt-0.5 h-4 w-4 shrink-0 ${item.ok ? 'text-success' : 'text-muted'}`} />
                <div className="flex flex-col gap-0.5">
                  <span className="text-xs text-muted">{item.label}</span>
                  <span className="text-xs font-medium text-body">{item.value}</span>
                </div>
              </div>
            ))}
          </div>

          <div className="flex justify-end">
            <Button
              variant="primary"
              icon={<ArrowRight className="h-4 w-4" />}
              onClick={() => setStep('intent')}
            >
              {t('templates.scaffold.next')}
            </Button>
          </div>
        </div>
      )}

      {/* ── Step 2: Intent / parameters ────────────────────────────────── */}
      {step === 'intent' && (
        <div className="flex flex-col gap-4">
          <div className="grid grid-cols-1 gap-4 rounded-lg border border-edge bg-elevated p-5 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <TextField
                label={t('templates.scaffold.fields.serviceName') + ' *'}
                placeholder={t('catalog.aiScaffold.serviceNamePlaceholder', 'payment-api')}
                value={serviceName}
                onChange={e =>
                  setServiceName(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))
                }
                helperText={t('templates.scaffold.hints.serviceName')}
                size="sm"
                required
              />
            </div>

            <TextArea
              className="sm:col-span-2"
              label={t('templates.scaffold.fields.serviceDescription') + ' *'}
              placeholder={t('templates.scaffold.placeholders.serviceDescription')}
              value={serviceDescription}
              onChange={e => setServiceDescription(e.target.value)}
              rows={4}
              textareaClassName="resize-none"
              helperText={t('templates.scaffold.hints.descriptionTip')}
              required
            />

            <TextField
              label={t('templates.scaffold.fields.mainEntities')}
              placeholder={t('catalog.aiScaffold.entitiesPlaceholder', 'Payment, Refund, Statement')}
              value={mainEntities}
              onChange={e => setMainEntities(e.target.value)}
              size="sm"
            />

            <Select
              label={t('templates.scaffold.fields.languageOverride')}
              value={languageOverride}
              onChange={e => setLanguageOverride(e.target.value)}
              options={[
                { value: '', label: `${t('templates.scaffold.options.useTemplateDefault')} (${template?.language})` },
                { value: 'DotNet', label: '.NET' },
                { value: 'NodeJs', label: 'Node.js' },
                { value: 'Java', label: 'Java' },
                { value: 'Go', label: 'Go' },
                { value: 'Python', label: 'Python' },
              ]}
              size="sm"
            />

            <TextField
              label={t('templates.scaffold.fields.teamName')}
              placeholder={template?.defaultTeam}
              value={teamName}
              onChange={e => setTeamName(e.target.value)}
              size="sm"
            />

            <TextField
              label={t('templates.scaffold.fields.domain')}
              placeholder={template?.defaultDomain}
              value={domain}
              onChange={e => setDomain(e.target.value)}
              size="sm"
            />

            <TextArea
              className="sm:col-span-2"
              label={t('templates.scaffold.fields.additionalRequirements')}
              placeholder={t('templates.scaffold.placeholders.additionalRequirements')}
              value={additionalRequirements}
              onChange={e => setAdditionalRequirements(e.target.value)}
              rows={3}
              textareaClassName="resize-none"
            />
          </div>

          <div className="flex justify-between">
            <Button
              variant="outline"
              icon={<ArrowLeft className="h-4 w-4" />}
              onClick={() => setStep('template')}
            >
              {t('common.back')}
            </Button>
            <Button
              variant="primary"
              disabled={!serviceName || !serviceDescription}
              icon={<Zap className="h-4 w-4" />}
              onClick={handleGenerate}
            >
              {t('templates.scaffold.generate')}
            </Button>
          </div>
        </div>
      )}

      {/* ── Step 3: Generating ──────────────────────────────────────────── */}
      {step === 'generate' && (
        <div className="flex flex-col items-center gap-5 py-12">
          {generateMutation.isPending ? (
            <>
              <div className="relative flex items-center justify-center">
                <div className="absolute h-20 w-20 animate-ping rounded-full bg-accent/10" />
                <div className="flex h-16 w-16 items-center justify-center rounded-full bg-accent/20">
                  <Zap className="h-8 w-8 animate-pulse text-accent" />
                </div>
              </div>
              <div className="flex flex-col items-center gap-2">
                <p className="text-sm font-medium text-body">
                  {t('templates.scaffold.generating.title')}
                </p>
                <p className="text-xs text-muted">
                  {t('templates.scaffold.generating.subtitle')}
                </p>
              </div>
              <div className="flex flex-col gap-1 text-xs text-muted">
                {[
                  t('templates.scaffold.generating.step1'),
                  t('templates.scaffold.generating.step2'),
                  t('templates.scaffold.generating.step3'),
                  t('templates.scaffold.generating.step4'),
                ].map((s, i) => (
                  <div key={i} className="flex items-center gap-2">
                    <ChevronRight className="h-3 w-3" />
                    {s}
                  </div>
                ))}
              </div>
            </>
          ) : generateMutation.isError ? (
            <div className="flex flex-col items-center gap-3">
              <AlertCircle className="h-12 w-12 text-critical" />
              <p className="text-sm font-medium text-critical">{t('templates.scaffold.generateError')}</p>
              <p className="text-xs text-muted">
                {generateMutation.error instanceof Error
                  ? generateMutation.error.message
                  : t('templates.scaffold.generateErrorGeneric')}
              </p>
              <Button
                variant="primary"
                onClick={handleGenerate}
              >
                {t('templates.scaffold.retry')}
              </Button>
            </div>
          ) : null}
        </div>
      )}

      {/* ── Step 4: Review & download ───────────────────────────────────── */}
      {step === 'review' && result && (
        <div className="flex flex-col gap-4">
          {/* Summary banner */}
          <div
            className={`flex items-start gap-3 rounded-lg border p-4 ${
              result.isFallback
                ? 'border-warning/30 bg-warning/10'
                : 'border-success/30 bg-success/10'
            }`}
          >
            {result.isFallback ? (
              <AlertCircle className="mt-0.5 h-5 w-5 shrink-0 text-warning" />
            ) : (
              <CheckCircle2 className="mt-0.5 h-5 w-5 shrink-0 text-success" />
            )}
            <div className="flex flex-col gap-0.5">
              <p className={`text-sm font-medium ${result.isFallback ? 'text-warning' : 'text-success'}`}>
                {result.isFallback
                  ? t('templates.scaffold.review.fallbackTitle')
                  : t('templates.scaffold.review.successTitle', { count: result.files.length })}
              </p>
              <p className="text-xs text-muted">
                {result.isFallback
                  ? t('templates.scaffold.review.fallbackHint')
                  : t('templates.scaffold.review.successHint', {
                      service: result.serviceName,
                      language: result.language,
                    })}
              </p>
            </div>
          </div>

          {/* File explorer */}
          <div className="grid grid-cols-1 gap-0 overflow-hidden rounded-lg border border-edge lg:grid-cols-4">
            {/* Sidebar */}
            <div className="flex flex-col border-b border-edge bg-elevated p-3 lg:border-b-0 lg:border-r">
              <p className="mb-2 text-xs font-medium text-muted">
                {result.files.length} {t('templates.scaffold.review.files')}
              </p>
              <FileTree
                files={result.files}
                selected={selectedFile}
                onSelect={setSelectedFile}
              />
            </div>

            {/* File content */}
            <div className="flex flex-col lg:col-span-3">
              <div className="flex items-center justify-between border-b border-edge bg-elevated px-3 py-2">
                <code className="text-xs text-muted">
                  {result.files[selectedFile]?.path}
                </code>
                <Button
                  variant="ghost"
                  size="sm"
                  icon={<Copy className="h-3.5 w-3.5" />}
                  onClick={handleCopyFile}
                >
                  {copied ? t('common.copied') : t('common.copy')}
                </Button>
              </div>
              <pre className="flex-1 overflow-auto bg-elevated p-4 text-xs text-body max-h-[440px]">
                {result.files[selectedFile]?.content ?? ''}
              </pre>
            </div>
          </div>

          {/* Actions */}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <Button
              variant="outline"
              icon={<ArrowLeft className="h-4 w-4" />}
              onClick={() => setStep('intent')}
            >
              {t('templates.scaffold.review.regenerate')}
            </Button>

            <div className="flex gap-2">
              <Button
                variant="primary"
                className="bg-success hover:bg-success/90"
                icon={<Download className="h-4 w-4" />}
                onClick={handleDownloadZip}
              >
                {t('templates.scaffold.review.downloadZip')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
}
