import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
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
type MockServerFormat = 'wiremock' | 'json-server' | 'mirage';

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
    ServerStubs: <Server className="h-4 w-4 text-blue-400" />,
    MockServer: <Box className="h-4 w-4 text-purple-400" />,
    PostmanCollection: <FileJson className="h-4 w-4 text-orange-400" />,
    ContractTests: <TestTube2 className="h-4 w-4 text-green-400" />,
    ClientSdk: <Code2 className="h-4 w-4 text-cyan-400" />,
  };

  return (
    <div className="rounded-lg border border-neutral-800 bg-neutral-900 overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-neutral-800 bg-neutral-900/60">
        <div className="flex items-center gap-2">
          {icons[artifactType] ?? <FileCode className="h-4 w-4 text-neutral-400" />}
          <span className="text-sm font-medium text-neutral-100">{t(`artifactTypes.${artifactType}`, { defaultValue: artifactType })}</span>
          <span className="rounded-full bg-neutral-800 px-2 py-0.5 text-xs text-neutral-400">{files.length} {t('files')}</span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleCopy}
            className="flex items-center gap-1.5 rounded px-2.5 py-1.5 text-xs text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200 transition-colors"
          >
            {copied ? <CheckCircle className="h-3.5 w-3.5 text-emerald-400" /> : <Copy className="h-3.5 w-3.5" />}
            {copied ? t('copied') : t('copy')}
          </button>
          <button
            onClick={onDownload}
            className="flex items-center gap-1.5 rounded px-2.5 py-1.5 text-xs bg-blue-600/20 text-blue-300 border border-blue-600/30 hover:bg-blue-600/30 transition-colors"
          >
            <Download className="h-3.5 w-3.5" />
            {t('download')}
          </button>
        </div>
      </div>

      {/* File tabs */}
      <div className="flex gap-1 overflow-x-auto border-b border-neutral-800 bg-neutral-950/40 px-4 py-2">
        {files.map((f, i) => (
          <button
            key={i}
            onClick={() => setSelected(i)}
            className={`shrink-0 rounded px-2.5 py-1 text-xs transition-colors ${
              i === selected
                ? 'bg-blue-600/20 text-blue-300'
                : 'text-neutral-500 hover:text-neutral-300 hover:bg-neutral-800'
            }`}
          >
            {getLanguageIcon(f.language)} {f.fileName}
          </button>
        ))}
      </div>

      {/* Code view */}
      <div className="max-h-80 overflow-auto p-4">
        <pre className="text-xs text-neutral-300 whitespace-pre-wrap font-mono leading-relaxed">
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
    <div className="flex flex-col gap-6 p-6 max-w-screen-xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-neutral-100">{t('title')}</h1>
          <p className="mt-1 text-sm text-neutral-400">{t('subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <Zap className="h-5 w-5 text-blue-400" />
          <span className="text-sm text-neutral-400">{t('contractToCode')}</span>
        </div>
      </div>

      {/* Pipeline diagram */}
      <div className="rounded-lg border border-neutral-800 bg-neutral-900/40 px-6 py-4">
        <div className="flex items-center gap-3 flex-wrap">
          {[
            { label: t('steps.contract'), icon: <FileJson className="h-4 w-4 text-blue-400" /> },
            { label: t('steps.pipeline'), icon: <Play className="h-4 w-4 text-purple-400" /> },
            { label: t('steps.server'), icon: <Server className="h-4 w-4 text-green-400" /> },
            { label: t('steps.mock'), icon: <Box className="h-4 w-4 text-orange-400" /> },
            { label: t('steps.tests'), icon: <TestTube2 className="h-4 w-4 text-yellow-400" /> },
            { label: t('steps.sdk'), icon: <Code2 className="h-4 w-4 text-cyan-400" /> },
          ].flatMap((step, i, arr) => [
            <div key={`step-${i}`} className="flex items-center gap-1.5">
              {step.icon}
              <span className="text-xs text-neutral-300">{step.label}</span>
            </div>,
            i < arr.length - 1 ? (
              <ArrowRight key={`arrow-${i}`} className="h-3.5 w-3.5 text-neutral-600 shrink-0" />
            ) : null,
          ])}
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        {/* Configuration */}
        <div className="flex flex-col gap-4">
          <h2 className="text-sm font-semibold text-neutral-200 uppercase tracking-wider">{t('configuration')}</h2>

          {/* Service name */}
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium text-neutral-400">{t('serviceName')}</label>
            <input
              type="text"
              value={serviceName}
              onChange={e => setServiceName(e.target.value)}
              placeholder={t('serviceNamePlaceholder')}
              className="rounded-md border border-neutral-700 bg-neutral-900 px-3 py-2 text-sm text-neutral-100 placeholder-neutral-600 focus:border-blue-500 focus:outline-none"
            />
          </div>

          {/* Language */}
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium text-neutral-400">{t('targetLanguage')}</label>
            <select
              value={targetLanguage}
              onChange={e => setTargetLanguage(e.target.value as TargetLanguage)}
              className="rounded-md border border-neutral-700 bg-neutral-900 px-3 py-2 text-sm text-neutral-100 focus:border-blue-500 focus:outline-none"
            >
              {LANGUAGES.map(l => (
                <option key={l} value={l}>{l.charAt(0).toUpperCase() + l.slice(1)}</option>
              ))}
            </select>
          </div>

          {/* Artifact toggles */}
          <div className="flex flex-col gap-2">
            <label className="text-xs font-medium text-neutral-400">{t('artifacts')}</label>
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
                      ? 'border-blue-500/50 bg-blue-600/10 text-blue-300'
                      : 'border-neutral-700 bg-neutral-900 text-neutral-500 hover:text-neutral-300'
                  }`}
                >
                  {item.value
                    ? <CheckCircle className="h-3.5 w-3.5 text-emerald-400 shrink-0" />
                    : <XCircle className="h-3.5 w-3.5 text-neutral-600 shrink-0" />}
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
            className="flex items-center justify-center gap-2 rounded-md bg-blue-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-blue-500 disabled:cursor-not-allowed disabled:opacity-50 transition-colors mt-2"
          >
            {mutation.isPending ? (
              <><Loader2 className="h-4 w-4 animate-spin" /> {t('running')}</>
            ) : (
              <><Play className="h-4 w-4" /> {t('runPipeline')}</>
            )}
          </button>

          {mutation.isError && (
            <div className="flex items-center gap-2 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-300">
              <XCircle className="h-4 w-4 shrink-0" />
              {t('runError')}
            </div>
          )}
        </div>

        {/* Contract JSON input */}
        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-medium text-neutral-400">{t('contractJson')}</label>
          <textarea
            value={contractJson}
            onChange={e => setContractJson(e.target.value)}
            placeholder={t('contractJsonPlaceholder')}
            className="flex-1 min-h-72 rounded-md border border-neutral-700 bg-neutral-900 px-3 py-2 text-xs font-mono text-neutral-100 placeholder-neutral-600 focus:border-blue-500 focus:outline-none resize-none"
          />
        </div>
      </div>

      {/* Results */}
      {mutation.isSuccess && mutation.data && (
        <div className="flex flex-col gap-4">
          {/* Summary bar */}
          <div className="flex items-center gap-4 rounded-lg border border-emerald-500/30 bg-emerald-500/10 px-4 py-3">
            <CheckCircle className="h-5 w-5 text-emerald-400 shrink-0" />
            <div className="flex-1">
              <p className="text-sm font-medium text-emerald-300">
                {t('pipelineSuccess', { count: mutation.data.totalArtifacts })}
              </p>
              <p className="text-xs text-emerald-400/70">
                {t('duration', { ms: mutation.data.durationMs })}
              </p>
            </div>
            <button
              onClick={() => {
                const allFiles = mutation.data.artifacts.flatMap(a => a.files);
                downloadZip(allFiles, `${mutation.data.serviceName}-pipeline.zip`);
              }}
              className="flex items-center gap-1.5 rounded-md bg-emerald-600/20 border border-emerald-500/30 px-3 py-1.5 text-xs text-emerald-300 hover:bg-emerald-600/30 transition-colors"
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
        <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-neutral-800 py-12 text-center">
          <RefreshCw className="h-8 w-8 text-neutral-700" />
          <p className="text-sm text-neutral-500">{t('emptyState')}</p>
        </div>
      )}
    </div>
  );
}
