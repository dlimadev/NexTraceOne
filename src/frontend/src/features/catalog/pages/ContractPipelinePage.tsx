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
  CheckCircle2,
  XCircle,
  ArrowRight,
  Play,
  Code2,
  FileJson,
  RefreshCw,
} from 'lucide-react';
import client from '../../../api/client';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, Select, TextArea } from '../../../shared/ui';

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
    client.post('/catalog/contracts/pipeline/orchestrate', body).then(r => r.data),
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

  // Intentional taxonomy: purple and orange have no semantic DS token
  const icons: Record<string, React.ReactNode> = {
    ServerStubs: <Server className="h-4 w-4 text-accent" />,
    MockServer: <Box className="h-4 w-4 text-purple-400" />,
    PostmanCollection: <FileJson className="h-4 w-4 text-orange-400" />,
    ContractTests: <TestTube2 className="h-4 w-4 text-success" />,
    ClientSdk: <Code2 className="h-4 w-4 text-cyan" />,
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
          <Button
            variant="ghost"
            size="xs"
            onClick={handleCopy}
            icon={copied ? <CheckCircle2 className="h-3.5 w-3.5 text-success" /> : <Copy className="h-3.5 w-3.5" />}
          >
            {copied ? t('copied') : t('copy')}
          </Button>
          <Button
            variant="ghost"
            size="xs"
            onClick={onDownload}
            icon={<Download className="h-3.5 w-3.5" />}
            className="bg-accent/20 text-accent border border-accent/30 hover:bg-accent/30 hover:text-accent"
          >
            {t('download')}
          </Button>
        </div>
      </div>

      {/* File tabs */}
      <div className="flex gap-1 overflow-x-auto border-b border-edge bg-elevated/40 px-4 py-2">
        {files.map((f, i) => (
          <Button
            key={i}
            variant="ghost"
            size="xs"
            onClick={() => setSelected(i)}
            className={`shrink-0 ${
              i === selected
                ? 'bg-accent/20 text-accent hover:text-accent'
                : 'text-muted hover:text-body hover:bg-elevated'
            }`}
          >
            {getLanguageIcon(f.language)} {f.fileName}
          </Button>
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

  const languageOptions = LANGUAGES.map(l => ({
    value: l,
    label: l.charAt(0).toUpperCase() + l.slice(1),
  }));

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
            { label: t('steps.sdk'), icon: <Code2 className="h-4 w-4 text-cyan" /> },
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
          <TextField
            label={t('serviceName')}
            value={serviceName}
            onChange={e => setServiceName(e.target.value)}
            placeholder={t('serviceNamePlaceholder')}
            size="sm"
          />

          {/* Language */}
          <Select
            label={t('targetLanguage')}
            value={targetLanguage}
            onChange={e => setTargetLanguage(e.target.value as TargetLanguage)}
            options={languageOptions}
            size="sm"
          />

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
                <Button
                  key={item.key}
                  variant="ghost"
                  size="sm"
                  onClick={() => item.setter(!item.value)}
                  icon={
                    item.value
                      ? <CheckCircle2 className="h-3.5 w-3.5 text-success shrink-0" />
                      : <XCircle className="h-3.5 w-3.5 text-muted shrink-0" />
                  }
                  className={`w-full justify-start ${
                    item.value
                      ? 'border border-accent/50 bg-accent/10 text-accent hover:bg-accent/15 hover:text-accent'
                      : 'border border-edge bg-elevated text-muted hover:text-body'
                  }`}
                >
                  <span className="shrink-0">{item.icon}</span>
                  {item.label}
                </Button>
              ))}
            </div>
          </div>

          {/* Run button */}
          <Button
            variant="primary"
            onClick={() => mutation.mutate()}
            disabled={!canRun}
            loading={mutation.isPending}
            icon={<Play className="h-4 w-4" />}
            className="w-full mt-2"
          >
            {mutation.isPending ? t('running') : t('runPipeline')}
          </Button>

          {mutation.isError && (
            <div className="flex items-center gap-2 rounded-md border border-critical/30 bg-critical/10 px-3 py-2 text-sm text-critical">
              <XCircle className="h-4 w-4 shrink-0" />
              {t('runError')}
            </div>
          )}
        </div>

        {/* Contract JSON input */}
        <TextArea
          label={t('contractJson')}
          value={contractJson}
          onChange={e => setContractJson(e.target.value)}
          placeholder={t('contractJsonPlaceholder')}
          className="flex-1"
          textareaClassName="min-h-72 font-mono text-xs resize-none"
        />
      </div>

      {/* Results */}
      {mutation.isSuccess && mutation.data && (
        <div className="flex flex-col gap-4">
          {/* Summary bar */}
          <div className="flex items-center gap-4 rounded-lg border border-success/30 bg-success/10 px-4 py-3">
            <CheckCircle2 className="h-5 w-5 text-success shrink-0" />
            <div className="flex-1">
              <p className="text-sm font-medium text-success">
                {t('pipelineSuccess', { count: mutation.data.totalArtifacts })}
              </p>
              <p className="text-xs text-success/70">
                {t('duration', { ms: mutation.data.durationMs })}
              </p>
            </div>
            <Button
              variant="ghost"
              size="xs"
              onClick={() => {
                const allFiles = mutation.data.artifacts.flatMap(a => a.files);
                downloadZip(allFiles, `${mutation.data.serviceName}-pipeline.zip`);
              }}
              icon={<Download className="h-3.5 w-3.5" />}
              className="bg-success/20 border border-success/30 text-success hover:bg-success/30 hover:text-success"
            >
              {t('downloadAll')}
            </Button>
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
