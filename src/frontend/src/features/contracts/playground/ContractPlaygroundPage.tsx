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

  const { data: contractDetail, isLoading: loadingDetail } = useQuery({
    queryKey: ['contract-playground-detail', contractVersionId],
    queryFn: () => contractsApi.getVersionDetail(contractVersionId),
    enabled: !!contractVersionId,
  });

  const endpoints: Array<{ method: string; path: string; summary: string }> =
    contractDetail?.spec
      ? extractEndpoints(contractDetail.spec)
      : [];

  const executeMutation = useMutation({
    mutationFn: async () => {
      // Simulate mock execution based on contract spec
      const start = performance.now();
      await new Promise((r) => setTimeout(r, 100 + Math.random() * 200));
      const duration = Math.round(performance.now() - start);

      const mockBody = contractDetail?.spec
        ? generateMockResponse(contractDetail.spec, request.path, request.method)
        : '{}';

      return {
        statusCode: 200,
        headers: { 'Content-Type': 'application/json', 'X-Mock': 'true' },
        body: mockBody,
        durationMs: duration,
      } satisfies PlaygroundResponse;
    },
    onSuccess: (data) => setResponse(data),
  });

  const methods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'];

  const methodColors: Record<string, string> = {
    GET: 'text-green-400',
    POST: 'text-blue-400',
    PUT: 'text-orange-400',
    PATCH: 'text-yellow-400',
    DELETE: 'text-red-400',
    HEAD: 'text-purple-400',
    OPTIONS: 'text-gray-400',
  };

  return (
    <div className="min-h-screen bg-background px-6 py-6 text-body">
      {/* ─── Header ─── */}
      <div className="mb-6">
        <div className="flex items-center gap-2 mb-1">
          <Code2 size={20} className="text-accent" />
          <h1 className="text-lg font-semibold text-heading">
            {t('contracts.playground.title', 'Contract Playground')}
          </h1>
        </div>
        <p className="text-xs text-muted">
          {t(
            'contracts.playground.subtitle',
            'Interactive tester — validate your API contracts against mock responses generated from the spec.'
          )}
        </p>
      </div>

      {/* ─── Contract Selector ─── */}
      <div className="bg-panel border border-edge rounded-lg p-4 mb-4">
        <label className="text-[10px] uppercase tracking-wider text-muted font-medium mb-1.5 block">
          {t('contracts.playground.contractId', 'Contract Version ID')}
        </label>
        <div className="flex gap-2">
          <input
            type="text"
            value={contractVersionId}
            onChange={(e) => setContractVersionId(e.target.value)}
            placeholder={t('contracts.playground.contractIdPlaceholder', 'Paste or select a contract version ID...')}
            className="flex-1 text-xs bg-elevated border border-edge rounded px-3 py-1.5 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
          />
        </div>
        {loadingDetail && (
          <p className="text-[10px] text-muted mt-1">{t('common.loading', 'Loading...')}</p>
        )}
        {contractDetail && (
          <p className="text-[10px] text-accent mt-1">
            {contractDetail.protocol} — v{contractDetail.semVer}
          </p>
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
              <button
                key={idx}
                type="button"
                onClick={() =>
                  setRequest((prev) => ({ ...prev, method: ep.method, path: ep.path }))
                }
                className="w-full flex items-center gap-2 text-xs px-2 py-1 rounded hover:bg-elevated/50 transition-colors text-left"
              >
                <span className={`font-mono font-bold text-[10px] w-14 ${methodColors[ep.method] ?? 'text-body'}`}>
                  {ep.method}
                </span>
                <span className="font-mono text-body">{ep.path}</span>
                {ep.summary && <span className="text-muted ml-auto truncate max-w-[200px]">{ep.summary}</span>}
              </button>
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

          <div className="flex gap-2 mb-3">
            <select
              value={request.method}
              onChange={(e) => setRequest((prev) => ({ ...prev, method: e.target.value }))}
              className="text-xs bg-elevated border border-edge rounded px-2 py-1.5 text-body focus:outline-none focus:border-accent"
            >
              {methods.map((m) => (
                <option key={m} value={m}>{m}</option>
              ))}
            </select>
            <input
              type="text"
              value={request.path}
              onChange={(e) => setRequest((prev) => ({ ...prev, path: e.target.value }))}
              placeholder={t('contracts.playground.placeholder.path', '/api/v1/resource')}
              className="flex-1 text-xs font-mono bg-elevated border border-edge rounded px-3 py-1.5 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
            />
            <button
              type="button"
              onClick={() => executeMutation.mutate()}
              disabled={!contractVersionId || executeMutation.isPending}
              className="flex items-center gap-1.5 text-xs font-medium bg-accent text-white rounded px-3 py-1.5 hover:bg-accent/90 transition-colors disabled:opacity-40"
            >
              <Play size={12} />
              {t('contracts.playground.send', 'Send')}
            </button>
          </div>

          {/* Headers toggle */}
          <button
            type="button"
            onClick={() => setShowHeaders(!showHeaders)}
            className="flex items-center gap-1 text-[10px] text-muted hover:text-body transition-colors mb-2"
          >
            {showHeaders ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
            {t('contracts.playground.headers', 'Headers')}
          </button>

          {showHeaders && (
            <textarea
              value={JSON.stringify(request.headers, null, 2)}
              onChange={(e) => {
                try {
                  setRequest((prev) => ({ ...prev, headers: JSON.parse(e.target.value) }));
                } catch { /* user is typing */ }
              }}
              rows={4}
              className="w-full text-xs font-mono bg-elevated border border-edge rounded px-3 py-2 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent mb-3"
            />
          )}

          {/* Body */}
          {['POST', 'PUT', 'PATCH'].includes(request.method) && (
            <>
              <label className="text-[10px] uppercase tracking-wider text-muted font-medium mb-1.5 block">
                {t('contracts.playground.body', 'Body')}
              </label>
              <textarea
                value={request.body}
                onChange={(e) => setRequest((prev) => ({ ...prev, body: e.target.value }))}
                rows={8}
                placeholder={t('contracts.playground.placeholder.body', '{}')}
                className="w-full text-xs font-mono bg-elevated border border-edge rounded px-3 py-2 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
              />
            </>
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
                    response.statusCode < 300 ? 'text-green-400' : response.statusCode < 500 ? 'text-orange-400' : 'text-red-400'
                  }`}
                >
                  {response.statusCode}
                </span>
                <span className="text-[10px] text-muted flex items-center gap-0.5">
                  <Clock size={10} />
                  {response.durationMs}ms
                </span>
                <button
                  type="button"
                  onClick={() => navigator.clipboard.writeText(response.body)}
                  className="text-muted hover:text-body transition-colors"
                  title={t('common.copy', 'Copy')}
                >
                  <Copy size={12} />
                </button>
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
                  <CheckCircle2 size={12} className="text-green-400" />
                ) : (
                  <XCircle size={12} className="text-red-400" />
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
    </div>
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
