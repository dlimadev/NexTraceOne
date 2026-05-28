import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import {
  Zap,
  FileCode,
  Server,
  TestTube2,
  Box,
  Download,
  Copy,
  CheckCircle,
  XCircle,
  Loader2,
  ArrowRight,
  Play,
  Code2,
  FileJson,
  RefreshCw,
} from 'lucide-react';
import axios from 'axios';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

// ── Types ──────────────────────────────────────────────────────────────────────

interface GeneratedFile {
  fileName: string;
  content: string;
  language: string;
  description: string;
}

interface PipelineResult {
  artifacts: {
    artifactType: string;
    files: GeneratedFile[];
  }[];
  totalArtifacts: number;
  durationMs: number;
  contractVersionId: string;
  serviceName: string;
  targetLanguage: string;
}

type TargetLanguage = 'dotnet' | 'nodejs' | 'java' | 'go' | 'python';

// ── API ────────────────────────────────────────────────────────────────────────

const pipelineApi = {
  orchestrate: (body: {
    contractVersionId: string;
    contractJson: string;
    serviceName: string;
    targetLanguage: TargetLanguage;
    generateServer: boolean;
    generateMockServer: boolean;
    generatePostman: boolean;
    generateTests: boolean;
    generateClientSdk: boolean;
  }): Promise<PipelineResult> =>
    axios.post('/api/v1/catalog/contracts/pipeline/orchestrate', body).then(r => r.data),
};

// ── Helpers ────────────────────────────────────────────────────────────────────

function getLanguageIcon(lang: string) {
  const l = lang.toLowerCase();
  if (l === 'csharp' || l === 'dotnet') return '🟣';
  if (l === 'javascript' || l === 'typescript') return '🟡';
  if (l === 'java') return '🟠';
  if (l === 'go') return '🔵';
  if (l === 'python') return '🐍';
  return '📄';
}

function downloadZip(files: GeneratedFile[], zipName: string) {
  import('jszip').then(({ default: JSZip }) => {
    const zip = new JSZip();
    files.forEach(f => zip.file(f.fileName, f.content));
    zip.generateAsync({ type: 'blob' }).then(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = zipName;
      a.click();
      URL.revokeObjectURL(url);
    });
  });
}

// ── Components ─────────────────────────────────────────────────────────────────

function ArtifactCard({
  artifactType,
  files,
  onDownload,
}: {
  artifactType: string;
  files: GeneratedFile[];
  onDownload: () => void;
}) {
  const { t } = useTranslation('contractPipeline');
  const [selected, setSelected] = useState(0);
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(files[selected]?.content ?? '');
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const icons: Record<string, React.ReactNode> = {
    ServerStubs: <Server className="h-4 w-4 text-accent" />,
    MockServer: <Box className="h-4 w-4 text-purple-400" />,
    PostmanCollection: <FileJson className="h-4 w-4 text-orange-400" />,
    ContractTests: <TestTube2 className="h-4 w-4 text-success" />,
    ClientSdk: <Code2 className="h-4 w-4 text-cyan-400" />,
  };

  return (
    <div className="rounded-lg border border-edge bg-elevated overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-edge bg-elevated/60">
        <div className="flex items-center gap-2">
          {icons[artifactType] ?? <FileCode className="h-4 w-4 text-muted" />}
          <span className="text-sm font-medium text-body">{t(`artifactTypes.${artifactType}`, { defaultValue: artifactType })}</span>
          <span className="rounded-full bg-elevated px-2 py-0.5 text-xs text-muted">{files.length} {t('files')}</span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleCopy}
            className="flex items-center gap-1.5 rounded px-2.5 py-1.5 text-xs text-muted hover:bg-elevated hover:text-body transition-colors"
          >
            {copied ? <CheckCircle className="h-3.5 w-3.5 text-success" /> : <Copy className="h-3.5 w-3.5" />}
            {copied ? t('copied') : t('copy')}
          </button>
          <button
            onClick={onDownload}
            className="flex items-center gap-1.5 rounded px-2.5 py-1.5 text-xs bg-accent/20 text-accent border border-accent/30 hover:bg-accent/30 transition-colors"
          >
            <Download className="h-3.5 w-3.5" />
            {t('download')}
          </button>
        </div>
      </div>

      {/* File tabs */}
      <div className="flex gap-1 overflow-x-auto border-b border-edge bg-elevated/40 px-4 py-2">
        {files.map((f, i) => (
          <button
            key={i}
            onClick={() => setSelected(i)}
            className={`shrink-0 rounded px-2.5 py-1 text-xs transition-colors ${
              i === selected
                ? 'bg-accent/20 text-accent'
                : 'text-muted hover:text-body hover:bg-elevated'
            }`}
          >
            {getLanguageIcon(f.language)} {f.fileName}
          </button>
        ))}
      </div>

      {/* Code view */}
      <div className="max-h-80 overflow-auto p-4">
        <pre className="text-xs text-body whitespace-pre-wrap font-mono leading-relaxed">
          {files[selected]?.content ?? ''}
        </pre>
      </div>
    </div>
  );
}

// ── Main Page ──────────────────────────────────────────────────────────────────

const LANGUAGES: TargetLanguage[] = ['dotnet', 'nodejs', 'java', 'go', 'python'];

export function ContractPipelinePage() {
  const { t } = useTranslation('contractPipeline');

  const [contractJson, setContractJson] = useState('');
  const [serviceName, setServiceName] = useState('');
  const [targetLanguage, setTargetLanguage] = useState<TargetLanguage>('dotnet');
  const [generateServer, setGenerateServer] = useState(true);
  const [generateMock, setGenerateMock] = useState(true);
  const [generatePostman, setGeneratePostman] = useState(true);
  const [generateTests, setGenerateTests] = useState(true);
  const [generateSdk, setGenerateSdk] = useState(false);
  const [contractVersionId] = useState(() => crypto.randomUUID());

  const mutation = useMutation({
    mutationFn: () =>
      pipelineApi.orchestrate({
        contractVersionId,
        contractJson,
        serviceName,
        targetLanguage,
        generateServer,
        generateMockServer: generateMock,
        generatePostman,
        generateTests,
        generateClientSdk: generateSdk,
      }),
  });

  const canRun = contractJson.trim().length > 0 && serviceName.trim().length > 0;

  return (
    <PageContainer>
      <PageHeader
        title={t('catalog.contractPipeline.title', 'Contract Pipeline')}
        subtitle={t('catalog.contractPipeline.subtitle', 'Generate server stubs, mocks, tests and SDKs from a contract')}
      />
      {/* Pipeline label */}
      <div className="flex items-center gap-2">
        <Zap className="h-5 w-5 text-accent" />
        <span className="text-sm text-muted">{t('contractToCode')}</span>
      </div>

      {/* Pipeline diagram */}
      <div className="rounded-lg border border-edge bg-elevated/40 px-6 py-4">
        <div className="flex items-center gap-3 flex-wrap">
          {[
            { label: t('steps.contract'), icon: <FileJson className="h-4 w-4 text-accent" /> },
            { label: t('steps.pipeline'), icon: <Play className="h-4 w-4 text-purple-400" /> },
            { label: t('steps.server'), icon: <Server className="h-4 w-4 text-success" /> },
            { label: t('steps.mock'), icon: <Box className="h-4 w-4 text-orange-400" /> },
            { label: t('steps.tests'), icon: <TestTube2 className="h-4 w-4 text-warning" /> },
            { label: t('steps.sdk'), icon: <Code2 className="h-4 w-4 text-cyan-400" /> },
          ].flatMap((step, i, arr) => [
            <div key={`step-${i}`} className="flex items-center gap-1.5">
              {step.icon}
              <span className="text-xs text-body">{step.label}</span>
            </div>,
            i < arr.length - 1 ? (
              <ArrowRight key={`arrow-${i}`} className="h-3.5 w-3.5 text-muted shrink-0" />
            ) : null,
          ])}
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        {/* Configuration */}
        <div className="flex flex-col gap-4">
          <h2 className="text-sm font-semibold text-body uppercase tracking-wider">{t('configuration')}</h2>

          {/* Service name */}
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium text-muted">{t('serviceName')}</label>
            <input
              type="text"
              value={serviceName}
              onChange={e => setServiceName(e.target.value)}
              placeholder={t('serviceNamePlaceholder')}
              className="rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:border-accent focus:outline-none"
            />
          </div>

          {/* Language */}
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium text-muted">{t('targetLanguage')}</label>
            <select
              value={targetLanguage}
              onChange={e => setTargetLanguage(e.target.value as TargetLanguage)}
              className="rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body focus:border-accent focus:outline-none"
            >
              {LANGUAGES.map(l => (
                <option key={l} value={l}>{l.charAt(0).toUpperCase() + l.slice(1)}</option>
              ))}
            </select>
          </div>

          {/* Artifact toggles */}
          <div className="flex flex-col gap-2">
            <label className="text-xs font-medium text-muted">{t('artifacts')}</label>
            <div className="grid grid-cols-2 gap-2">
              {[
                { key: 'server', label: t('artifactTypes.ServerStubs'), value: generateServer, setter: setGenerateServer, icon: <Server className="h-3.5 w-3.5" /> },
                { key: 'mock', label: t('artifactTypes.MockServer'), value: generateMock, setter: setGenerateMock, icon: <Box className="h-3.5 w-3.5" /> },
                { key: 'postman', label: t('artifactTypes.PostmanCollection'), value: generatePostman, setter: setGeneratePostman, icon: <FileJson className="h-3.5 w-3.5" /> },
                { key: 'tests', label: t('artifactTypes.ContractTests'), value: generateTests, setter: setGenerateTests, icon: <TestTube2 className="h-3.5 w-3.5" /> },
                { key: 'sdk', label: t('artifactTypes.ClientSdk'), value: generateSdk, setter: setGenerateSdk, icon: <Code2 className="h-3.5 w-3.5" /> },
              ].map(item => (
                <button
                  key={item.key}
                  onClick={() => item.setter(!item.value)}
                  className={`flex items-center gap-2 rounded-md border px-3 py-2 text-xs transition-colors ${
                    item.value
                      ? 'border-accent/50 bg-accent/10 text-accent'
                      : 'border-edge bg-elevated text-muted hover:text-body'
                  }`}
                >
                  {item.value
                    ? <CheckCircle className="h-3.5 w-3.5 text-success shrink-0" />
                    : <XCircle className="h-3.5 w-3.5 text-muted shrink-0" />}
                  {item.icon}
                  {item.label}
                </button>
              ))}
            </div>
          </div>

          {/* Run button */}
          <button
            onClick={() => mutation.mutate()}
            disabled={!canRun || mutation.isPending}
            className="flex items-center justify-center gap-2 rounded-md bg-accent px-4 py-2.5 text-sm font-medium text-on-accent hover:bg-accent/90 disabled:cursor-not-allowed disabled:opacity-50 transition-colors mt-2"
          >
            {mutation.isPending ? (
              <><Loader2 className="h-4 w-4 animate-spin" /> {t('running')}</>
            ) : (
              <><Play className="h-4 w-4" /> {t('runPipeline')}</>
            )}
          </button>

          {mutation.isError && (
            <div className="flex items-center gap-2 rounded-md border border-critical/30 bg-critical/10 px-3 py-2 text-sm text-critical">
              <XCircle className="h-4 w-4 shrink-0" />
              {t('runError')}
            </div>
          )}
        </div>

        {/* Contract JSON input */}
        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-medium text-muted">{t('contractJson')}</label>
          <textarea
            value={contractJson}
            onChange={e => setContractJson(e.target.value)}
            placeholder={t('contractJsonPlaceholder')}
            className="flex-1 min-h-72 rounded-md border border-edge bg-elevated px-3 py-2 text-xs font-mono text-body placeholder-muted focus:border-accent focus:outline-none resize-none"
          />
        </div>
      </div>

      {/* Results */}
      {mutation.isSuccess && mutation.data && (
        <div className="flex flex-col gap-4">
          {/* Summary bar */}
          <div className="flex items-center gap-4 rounded-lg border border-success/30 bg-success/10 px-4 py-3">
            <CheckCircle className="h-5 w-5 text-success shrink-0" />
            <div className="flex-1">
              <p className="text-sm font-medium text-success">
                {t('pipelineSuccess', { count: mutation.data.totalArtifacts })}
              </p>
              <p className="text-xs text-success/70">
                {t('duration', { ms: mutation.data.durationMs })}
              </p>
            </div>
            <button
              onClick={() => {
                const allFiles = mutation.data.artifacts.flatMap(a => a.files);
                downloadZip(allFiles, `${mutation.data.serviceName}-pipeline.zip`);
              }}
              className="flex items-center gap-1.5 rounded-md bg-success/20 border border-success/30 px-3 py-1.5 text-xs text-success hover:bg-success/30 transition-colors"
            >
              <Download className="h-3.5 w-3.5" />
              {t('downloadAll')}
            </button>
          </div>

          {/* Artifact cards */}
          <div className="grid grid-cols-1 gap-4">
            {mutation.data.artifacts.map(a => (
              <ArtifactCard
                key={a.artifactType}
                artifactType={a.artifactType}
                files={a.files}
                onDownload={() => downloadZip(a.files, `${a.artifactType.toLowerCase()}.zip`)}
              />
            ))}
          </div>
        </div>
      )}

      {/* Empty state */}
      {!mutation.isSuccess && !mutation.isPending && (
        <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-edge py-12 text-center">
          <RefreshCw className="h-8 w-8 text-muted" />
          <p className="text-sm text-muted">{t('emptyState')}</p>
        </div>
      )}
    </PageContainer>
  );
}
