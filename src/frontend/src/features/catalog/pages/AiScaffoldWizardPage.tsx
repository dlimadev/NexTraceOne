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
  CheckCircle,
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
          ? 'bg-emerald-500 text-white'
          : active
          ? 'bg-blue-600 text-white ring-2 ring-blue-500/30'
          : 'bg-neutral-800 text-neutral-500'
      }`}
    >
      {done ? <CheckCircle className="h-4 w-4" /> : stepIdx + 1}
    </div>
  );
}

function StepLine({ done }: { done: boolean }) {
  return (
    <div
      className={`h-px flex-1 transition-colors ${done ? 'bg-emerald-500' : 'bg-neutral-800'}`}
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
        <button
          key={i}
          onClick={() => onSelect(i)}
          className={`flex items-center gap-2 truncate rounded px-2 py-1 text-left text-xs transition-colors ${
            i === selected
              ? 'bg-blue-600/20 text-blue-300'
              : 'text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200'
          }`}
        >
          <FileCode className="h-3 w-3 shrink-0" />
          <span className="truncate">{f.path}</span>
        </button>
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
        <Loader2 className="h-8 w-8 animate-spin text-neutral-500" />
      </div>
    );
  }

  const INPUT_CLASS =
    'w-full rounded border border-neutral-700 bg-neutral-800 px-3 py-2 text-sm text-neutral-200 placeholder-neutral-500 outline-none focus:border-blue-500';

  return (
    <div className="flex flex-col gap-5 p-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate(`/catalog/templates/${id}`)}
          className="flex items-center gap-1.5 text-sm text-neutral-400 hover:text-neutral-200"
        >
          <ArrowLeft className="h-4 w-4" />
          {t('templates.scaffold.backToTemplate')}
        </button>
        <div className="flex items-center gap-2">
          <Zap className="h-5 w-5 text-blue-400" />
          <h1 className="text-lg font-semibold text-neutral-100">{t('templates.scaffold.title')}</h1>
        </div>
      </div>

      {/* Step indicator */}
      <div className="flex items-center gap-2 rounded-lg border border-neutral-800 bg-neutral-900 p-4">
        {STEPS.map((s, i) => (
          <div key={s} className="flex flex-1 items-center gap-2">
            <StepDot step={s} current={step} />
            <span
              className={`hidden text-xs font-medium sm:block ${
                s === step ? 'text-neutral-100' : 'text-neutral-500'
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
          <div className="rounded-lg border border-neutral-800 bg-neutral-900 p-5">
            <div className="mb-3 flex items-start justify-between">
              <div>
                <h2 className="text-sm font-semibold text-neutral-100">{template.displayName}</h2>
                <p className="text-xs text-neutral-500">{template.slug} · v{template.version}</p>
              </div>
              <div className="flex gap-1.5">
                <span className="rounded border border-purple-500/20 bg-purple-500/10 px-1.5 py-0.5 text-xs text-purple-400">
                  {template.language}
                </span>
                <span className="rounded border border-blue-500/20 bg-blue-500/10 px-1.5 py-0.5 text-xs text-blue-400">
                  {template.serviceType}
                </span>
              </div>
            </div>
            <p className="text-sm text-neutral-400">{template.description}</p>
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
                className="flex items-start gap-3 rounded-lg border border-neutral-800 bg-neutral-900 p-3"
              >
                <item.icon className={`mt-0.5 h-4 w-4 shrink-0 ${item.ok ? 'text-emerald-400' : 'text-neutral-500'}`} />
                <div className="flex flex-col gap-0.5">
                  <span className="text-xs text-neutral-500">{item.label}</span>
                  <span className="text-xs font-medium text-neutral-300">{item.value}</span>
                </div>
              </div>
            ))}
          </div>

          <div className="flex justify-end">
            <button
              className="flex items-center gap-2 rounded bg-blue-600 px-5 py-2 text-sm font-medium text-white hover:bg-blue-500"
              onClick={() => setStep('intent')}
            >
              {t('templates.scaffold.next')}
              <ArrowRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}

      {/* ── Step 2: Intent / parameters ────────────────────────────────── */}
      {step === 'intent' && (
        <div className="flex flex-col gap-4">
          <div className="grid grid-cols-1 gap-4 rounded-lg border border-neutral-800 bg-neutral-900 p-5 sm:grid-cols-2">
            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <label className="text-xs font-medium text-neutral-300">
                {t('templates.scaffold.fields.serviceName')}
                <span className="ml-0.5 text-red-400">*</span>
              </label>
              <input
                className={INPUT_CLASS}
                placeholder="payment-api"
                value={serviceName}
                onChange={e =>
                  setServiceName(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))
                }
              />
              <p className="text-xs text-neutral-500">{t('templates.scaffold.hints.serviceName')}</p>
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <label className="text-xs font-medium text-neutral-300">
                {t('templates.scaffold.fields.serviceDescription')}
                <span className="ml-0.5 text-red-400">*</span>
              </label>
              <textarea
                className={`${INPUT_CLASS} resize-none`}
                rows={4}
                placeholder={t('templates.scaffold.placeholders.serviceDescription')}
                value={serviceDescription}
                onChange={e => setServiceDescription(e.target.value)}
              />
              <p className="text-xs text-neutral-500">{t('templates.scaffold.hints.descriptionTip')}</p>
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-neutral-300">
                {t('templates.scaffold.fields.mainEntities')}
              </label>
              <input
                className={INPUT_CLASS}
                placeholder="Payment, Refund, Statement"
                value={mainEntities}
                onChange={e => setMainEntities(e.target.value)}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-neutral-300">
                {t('templates.scaffold.fields.languageOverride')}
              </label>
              <select
                className={INPUT_CLASS}
                value={languageOverride}
                onChange={e => setLanguageOverride(e.target.value)}
              >
                <option value="">{t('templates.scaffold.options.useTemplateDefault')} ({template?.language})</option>
                <option value="DotNet">.NET</option>
                <option value="NodeJs">Node.js</option>
                <option value="Java">Java</option>
                <option value="Go">Go</option>
                <option value="Python">Python</option>
              </select>
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-neutral-300">
                {t('templates.scaffold.fields.teamName')}
              </label>
              <input
                className={INPUT_CLASS}
                placeholder={template?.defaultTeam}
                value={teamName}
                onChange={e => setTeamName(e.target.value)}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-neutral-300">
                {t('templates.scaffold.fields.domain')}
              </label>
              <input
                className={INPUT_CLASS}
                placeholder={template?.defaultDomain}
                value={domain}
                onChange={e => setDomain(e.target.value)}
              />
            </div>

            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <label className="text-xs font-medium text-neutral-300">
                {t('templates.scaffold.fields.additionalRequirements')}
              </label>
              <textarea
                className={`${INPUT_CLASS} resize-none`}
                rows={3}
                placeholder={t('templates.scaffold.placeholders.additionalRequirements')}
                value={additionalRequirements}
                onChange={e => setAdditionalRequirements(e.target.value)}
              />
            </div>
          </div>

          <div className="flex justify-between">
            <button
              className="flex items-center gap-2 rounded border border-neutral-700 bg-neutral-800 px-4 py-2 text-sm font-medium text-neutral-300 hover:bg-neutral-700"
              onClick={() => setStep('template')}
            >
              <ArrowLeft className="h-4 w-4" />
              {t('common.back')}
            </button>
            <button
              disabled={!serviceName || !serviceDescription}
              className="flex items-center gap-2 rounded bg-blue-600 px-5 py-2 text-sm font-medium text-white hover:bg-blue-500 disabled:opacity-40"
              onClick={handleGenerate}
            >
              <Zap className="h-4 w-4" />
              {t('templates.scaffold.generate')}
            </button>
          </div>
        </div>
      )}

      {/* ── Step 3: Generating ──────────────────────────────────────────── */}
      {step === 'generate' && (
        <div className="flex flex-col items-center gap-5 py-12">
          {generateMutation.isPending ? (
            <>
              <div className="relative flex items-center justify-center">
                <div className="absolute h-20 w-20 animate-ping rounded-full bg-blue-500/10" />
                <div className="flex h-16 w-16 items-center justify-center rounded-full bg-blue-600/20">
                  <Zap className="h-8 w-8 animate-pulse text-blue-400" />
                </div>
              </div>
              <div className="flex flex-col items-center gap-2">
                <p className="text-sm font-medium text-neutral-200">
                  {t('templates.scaffold.generating.title')}
                </p>
                <p className="text-xs text-neutral-500">
                  {t('templates.scaffold.generating.subtitle')}
                </p>
              </div>
              <div className="flex flex-col gap-1 text-xs text-neutral-600">
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
              <AlertCircle className="h-12 w-12 text-red-400" />
              <p className="text-sm font-medium text-red-400">{t('templates.scaffold.generateError')}</p>
              <p className="text-xs text-neutral-500">
                {generateMutation.error instanceof Error
                  ? generateMutation.error.message
                  : t('templates.scaffold.generateErrorGeneric')}
              </p>
              <button
                className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-500"
                onClick={handleGenerate}
              >
                {t('templates.scaffold.retry')}
              </button>
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
                ? 'border-amber-500/30 bg-amber-500/10'
                : 'border-emerald-500/30 bg-emerald-500/10'
            }`}
          >
            {result.isFallback ? (
              <AlertCircle className="mt-0.5 h-5 w-5 shrink-0 text-amber-400" />
            ) : (
              <CheckCircle className="mt-0.5 h-5 w-5 shrink-0 text-emerald-400" />
            )}
            <div className="flex flex-col gap-0.5">
              <p className={`text-sm font-medium ${result.isFallback ? 'text-amber-300' : 'text-emerald-300'}`}>
                {result.isFallback
                  ? t('templates.scaffold.review.fallbackTitle')
                  : t('templates.scaffold.review.successTitle', { count: result.files.length })}
              </p>
              <p className="text-xs text-neutral-400">
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
          <div className="grid grid-cols-1 gap-0 overflow-hidden rounded-lg border border-neutral-800 lg:grid-cols-4">
            {/* Sidebar */}
            <div className="flex flex-col border-b border-neutral-800 bg-neutral-950 p-3 lg:border-b-0 lg:border-r">
              <p className="mb-2 text-xs font-medium text-neutral-500">
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
              <div className="flex items-center justify-between border-b border-neutral-800 bg-neutral-950 px-3 py-2">
                <code className="text-xs text-neutral-400">
                  {result.files[selectedFile]?.path}
                </code>
                <button
                  onClick={handleCopyFile}
                  className="flex items-center gap-1.5 rounded px-2 py-1 text-xs text-neutral-500 hover:bg-neutral-800 hover:text-neutral-300"
                >
                  <Copy className="h-3.5 w-3.5" />
                  {copied ? t('common.copied') : t('common.copy')}
                </button>
              </div>
              <pre className="flex-1 overflow-auto bg-neutral-950 p-4 text-xs text-neutral-300 max-h-[440px]">
                {result.files[selectedFile]?.content ?? ''}
              </pre>
            </div>
          </div>

          {/* Actions */}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <button
              className="flex items-center gap-2 rounded border border-neutral-700 bg-neutral-800 px-4 py-2 text-sm font-medium text-neutral-300 hover:bg-neutral-700"
              onClick={() => setStep('intent')}
            >
              <ArrowLeft className="h-4 w-4" />
              {t('templates.scaffold.review.regenerate')}
            </button>

            <div className="flex gap-2">
              <button
                onClick={handleDownloadZip}
                className="flex items-center gap-2 rounded bg-emerald-600 px-5 py-2 text-sm font-medium text-white hover:bg-emerald-500"
              >
                <Download className="h-4 w-4" />
                {t('templates.scaffold.review.downloadZip')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
