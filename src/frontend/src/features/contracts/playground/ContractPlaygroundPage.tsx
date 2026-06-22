/**
 * ContractPlaygroundPage — testador interativo de contratos REST.
 * Permite ao utilizador enviar requests contra um mock baseado no contrato,
 * visualizar responses e validar comportamento esperado.
 * Pilar: Contract Governance + Developer Acceleration.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import {
  Play,
  Copy,
  CheckCircle2,
  XCircle,
  ChevronDown,
  ChevronRight,
  Code2,
  Send,
  FileJson,
  Clock,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, TextArea, Select } from '../../../shared/ui';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';

type PlaygroundRequest = {
  method: string;
  path: string;
  headers: Record<string, string>;
  body: string;
};

type PlaygroundResponse = {
  statusCode: number;
  headers: Record<string, string>;
  body: string;
  durationMs: number;
};

export function ContractPlaygroundPage() {
  const { t } = useTranslation();
  const [contractVersionId, setContractVersionId] = useState('');
  const [request, setRequest] = useState<PlaygroundRequest>({
    method: 'GET',
    path: '/',
    headers: { 'Content-Type': 'application/json' },
    body: '',
  });
  const [response, setResponse] = useState<PlaygroundResponse | null>(null);
  const [showHeaders, setShowHeaders] = useState(false);

  const {
    data: contractDetail,
    isLoading: loadingDetail,
    isError: errorDetail,
    refetch: refetchDetail,
  } = useQuery({
    queryKey: ['contract-playground-detail', contractVersionId],
    queryFn: () => contractsApi.getDetail(contractVersionId),
    enabled: !!contractVersionId,
  });

  const executeMutation = useMutation({
    mutationFn: async () => {
      // Simulate mock execution based on contract spec
      const start = performance.now();
      await new Promise((r) => setTimeout(r, 100 + Math.random() * 200));
      const duration = Math.round(performance.now() - start);

      const previewBody = contractDetail?.spec
        ? generateMockResponse(contractDetail.spec, request.path, request.method)
        : '{}';

      return {
        statusCode: 200,
        headers: { 'Content-Type': 'application/json', 'X-Mock': 'true' },
        body: previewBody,
        durationMs: duration,
      } satisfies PlaygroundResponse;
    },
    onSuccess: (data) => setResponse(data),
  });

  const methods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'];
  const methodOptions = methods.map((m) => ({ value: m, label: m }));

  const methodColors: Record<string, string> = {
    GET: 'text-success',
    POST: 'text-blue-400',
    PUT: 'text-warning',
    PATCH: 'text-yellow-400',
    DELETE: 'text-critical',
    HEAD: 'text-purple-400',
    OPTIONS: 'text-faded',
  };

  if (errorDetail) {
    return (
      <PageContainer>
        <PageErrorState onRetry={refetchDetail} />
      </PageContainer>
    );
  }

  const endpoints: Array<{ method: string; path: string; summary: string }> =
    contractDetail?.spec
      ? extractEndpoints(contractDetail.spec)
      : [];

  return (
    <PageContainer>
      <PageHeader
        title={t('contracts.playground.title', 'Contract Playground')}
        subtitle={t(
          'contracts.playground.subtitle',
          'Interactive tester — validate your API contracts against mock responses generated from the spec.'
        )}
        icon={<Code2 size={20} />}
      />

      {/* ─── Contract Selector ─── */}
      <div className="bg-panel border border-edge rounded-lg p-4 mb-4">
        <TextField
          label={t('contracts.playground.contractId', 'Contract Version ID')}
          size="sm"
          value={contractVersionId}
          onChange={(e) => setContractVersionId(e.target.value)}
          placeholder={t('contracts.playground.contractIdPlaceholder', 'Paste or select a contract version ID...')}
        />
        {loadingDetail && (
          <p className="text-[10px] text-muted mt-1">{t('common.loading', 'Loading...')}</p>
        )}
        {contractDetail && (
          <p className="text-[10px] text-accent mt-1">
            {contractDetail.protocol} — v{contractDetail.semVer}
          </p>
        )}
        {contractVersionId && !loadingDetail && !contractDetail && (
          <EmptyState
            title={t('common.noResults', 'No results found')}
            size="compact"
          />
        )}
      </div>

      {/* ─── Quick Endpoint Select ─── */}
      {endpoints.length > 0 && (
        <div className="bg-panel border border-edge rounded-lg p-4 mb-4">
          <h3 className="text-[10px] uppercase tracking-wider text-muted font-medium mb-2">
            {t('contracts.playground.endpoints', 'Available Endpoints')}
          </h3>
          <div className="space-y-1">
            {endpoints.map((ep, idx) => (
              <Button
                key={idx}
                variant="ghost"
                size="sm"
                className="w-full justify-start"
                onClick={() =>
                  setRequest((prev) => ({ ...prev, method: ep.method, path: ep.path }))
                }
              >
                <span className={`font-mono font-bold text-[10px] w-14 ${methodColors[ep.method] ?? 'text-body'}`}>
                  {ep.method}
                </span>
                <span className="font-mono text-body">{ep.path}</span>
                {ep.summary && <span className="text-muted ml-auto truncate max-w-[200px]">{ep.summary}</span>}
              </Button>
            ))}
          </div>
        </div>
      )}

      {/* ─── Request Builder ─── */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Left: Request */}
        <div className="bg-panel border border-edge rounded-lg p-4">
          <div className="flex items-center gap-2 mb-3">
            <Send size={14} className="text-accent" />
            <h2 className="text-xs font-semibold text-heading">
              {t('contracts.playground.request', 'Request')}
            </h2>
          </div>

          <div className="flex gap-2 mb-3 items-end">
            <Select
              options={methodOptions}
              size="sm"
              value={request.method}
              onChange={(e) => setRequest((prev) => ({ ...prev, method: e.target.value }))}
              className="w-28 shrink-0"
            />
            <div className="flex-1">
              <TextField
                size="sm"
                value={request.path}
                onChange={(e) => setRequest((prev) => ({ ...prev, path: e.target.value }))}
                placeholder={t('contracts.playground.placeholder.path', '/api/v1/resource')}
                className="font-mono"
              />
            </div>
            <Button
              variant="primary"
              size="sm"
              icon={<Play size={12} />}
              loading={executeMutation.isPending}
              disabled={!contractVersionId || executeMutation.isPending}
              onClick={() => executeMutation.mutate()}
            >
              {t('contracts.playground.send', 'Send')}
            </Button>
          </div>

          {/* Headers toggle */}
          <Button
            variant="ghost"
            size="xs"
            icon={showHeaders ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
            onClick={() => setShowHeaders(!showHeaders)}
            className="mb-2"
          >
            {t('contracts.playground.headers', 'Headers')}
          </Button>

          {showHeaders && (
            <TextArea
              value={JSON.stringify(request.headers, null, 2)}
              onChange={(e) => {
                try {
                  setRequest((prev) => ({ ...prev, headers: JSON.parse(e.target.value) }));
                } catch { /* user is typing */ }
              }}
              rows={4}
              textareaClassName="font-mono text-xs"
            />
          )}

          {/* Body */}
          {['POST', 'PUT', 'PATCH'].includes(request.method) && (
            <TextArea
              label={t('contracts.playground.body', 'Body')}
              value={request.body}
              onChange={(e) => setRequest((prev) => ({ ...prev, body: e.target.value }))}
              rows={8}
              placeholder={t('contracts.playground.placeholder.body', '{}')}
              textareaClassName="font-mono text-xs"
              className="mt-3"
            />
          )}
        </div>

        {/* Right: Response */}
        <div className="bg-panel border border-edge rounded-lg p-4">
          <div className="flex items-center gap-2 mb-3">
            <FileJson size={14} className="text-accent" />
            <h2 className="text-xs font-semibold text-heading">
              {t('contracts.playground.response', 'Response')}
            </h2>
            {response && (
              <div className="ml-auto flex items-center gap-2">
                <span
                  className={`text-xs font-bold ${
                    response.statusCode < 300
                      ? 'text-success'
                      : response.statusCode < 500
                        ? 'text-warning'
                        : 'text-critical'
                  }`}
                >
                  {response.statusCode}
                </span>
                <span className="text-[10px] text-muted flex items-center gap-0.5">
                  <Clock size={10} />
                  {response.durationMs}ms
                </span>
                <Button
                  variant="ghost"
                  size="xs"
                  icon={<Copy size={12} />}
                  onClick={() => navigator.clipboard.writeText(response.body)}
                  title={t('common.copy', 'Copy')}
                >
                  <span className="sr-only">{t('common.copy', 'Copy')}</span>
                </Button>
              </div>
            )}
          </div>

          {!response && (
            <div className="flex flex-col items-center justify-center h-48 text-muted">
              <Code2 size={24} className="mb-2 opacity-30" />
              <p className="text-xs">
                {t('contracts.playground.emptyResponse', 'Send a request to see the mock response here')}
              </p>
            </div>
          )}

          {response && (
            <div className="space-y-3">
              <div className="flex items-center gap-1.5 text-[10px]">
                {response.statusCode < 300 ? (
                  <CheckCircle2 size={12} className="text-success" />
                ) : (
                  <XCircle size={12} className="text-critical" />
                )}
                <span className="text-muted">
                  {t('contracts.playground.mockGenerated', 'Mock response generated from contract spec')}
                </span>
              </div>
              <pre className="text-xs font-mono bg-elevated border border-edge rounded p-3 overflow-auto max-h-[400px] text-body whitespace-pre-wrap">
                {formatJson(response.body)}
              </pre>
            </div>
          )}
        </div>
      </div>
    </PageContainer>
  );
}

/* ─── Helpers ─── */

function extractEndpoints(spec: string): Array<{ method: string; path: string; summary: string }> {
  try {
    const parsed = JSON.parse(spec);
    const paths = parsed.paths ?? {};
    const results: Array<{ method: string; path: string; summary: string }> = [];
    for (const [path, methods] of Object.entries(paths)) {
      if (typeof methods !== 'object' || methods === null) continue;
      for (const [method, op] of Object.entries(methods as Record<string, unknown>)) {
        if (['get', 'post', 'put', 'patch', 'delete', 'head', 'options'].includes(method)) {
          results.push({
            method: method.toUpperCase(),
            path,
            summary: (op as { summary?: string })?.summary ?? '',
          });
        }
      }
    }
    return results;
  } catch {
    return [];
  }
}

function generateMockResponse(spec: string, path: string, method: string): string {
  try {
    const parsed = JSON.parse(spec);
    const pathObj = parsed.paths?.[path];
    if (!pathObj) return JSON.stringify({ message: 'Path not found in spec' }, null, 2);
    const methodObj = pathObj[method.toLowerCase()];
    if (!methodObj) return JSON.stringify({ message: 'Method not found for path' }, null, 2);

    const response200 = methodObj.responses?.['200'] ?? methodObj.responses?.['201'];
    if (response200?.content?.['application/json']?.example) {
      return JSON.stringify(response200.content['application/json'].example, null, 2);
    }

    return JSON.stringify({ message: 'OK', mock: true, path, method }, null, 2);
  } catch {
    return JSON.stringify({ message: 'OK', mock: true }, null, 2);
  }
}

function formatJson(s: string): string {
  try {
    return JSON.stringify(JSON.parse(s), null, 2);
  } catch {
    return s;
  }
}
