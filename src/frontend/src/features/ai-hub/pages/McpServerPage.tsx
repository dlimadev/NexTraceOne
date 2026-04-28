import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Cpu, Plug, Tag, ChevronDown, ChevronUp, Copy, Check,
  Layers, Zap, RefreshCw, ExternalLink,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { CardListSkeleton } from '../../../components/CardListSkeleton';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Button } from '../../../components/Button';
import { aiGovernanceApi } from '../api';

interface McpServerInfo {
  serverName: string;
  protocolVersion: string;
  serverVersion: string;
  description: string;
  toolCount: number;
  categories: string[];
  endpointUrl: string;
  capabilities: {
    tools?: { listChanged: boolean } | null;
    prompts?: null;
    resources?: null;
  };
}

interface McpTool {
  name: string;
  description: string;
  inputSchema: {
    type: string;
    properties: Record<string, { type: string; description: string }>;
    required: string[];
  };
}

interface McpToolsResponse {
  tools: McpTool[];
  totalCount: number;
}

/**
 * Página MCP Server — estado do servidor MCP nativo do NexTraceOne.
 * Exibe metadados do servidor, tools disponíveis e instruções de ligação.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function McpServerPage() {
  const { t } = useTranslation();
  const [expandedTool, setExpandedTool] = useState<string | null>(null);
  const [copiedEndpoint, setCopiedEndpoint] = useState(false);

  const {
    data: serverInfo,
    isLoading: isLoadingInfo,
    isError: isInfoError,
    refetch: refetchInfo,
  } = useQuery<McpServerInfo>({
    queryKey: ['ai', 'mcp', 'server-info'],
    queryFn: () => aiGovernanceApi.getMcpServerInfo(),
    staleTime: 60_000,
  });

  const {
    data: toolsData,
    isLoading: isLoadingTools,
    isError: isToolsError,
    refetch: refetchTools,
  } = useQuery<McpToolsResponse>({
    queryKey: ['ai', 'mcp', 'tools'],
    queryFn: () => aiGovernanceApi.listMcpTools(),
    staleTime: 60_000,
  });

  const isLoading = isLoadingInfo || isLoadingTools;
  const isError = isInfoError || isToolsError;

  const handleCopyEndpoint = () => {
    if (!serverInfo) return;
    const fullUrl = `${window.location.origin}/api/v1${serverInfo.endpointUrl}`;
    navigator.clipboard.writeText(fullUrl).then(() => {
      setCopiedEndpoint(true);
      setTimeout(() => setCopiedEndpoint(false), 2000);
    });
  };

  const handleRefresh = () => {
    refetchInfo();
    refetchTools();
  };

  if (isLoading) {
    return (
      <PageContainer>
        <PageHeader
          title={t('mcpServer.title')}
          subtitle={t('mcpServer.subtitle')}
        />
        <CardListSkeleton count={3} />
      </PageContainer>
    );
  }

  if (isError || !serverInfo) {
    return (
      <PageContainer>
        <PageHeader
          title={t('mcpServer.title')}
          subtitle={t('mcpServer.subtitle')}
        />
        <PageErrorState
          title={t('mcpServer.errorTitle')}
          message={t('mcpServer.errorMessage')}
          onRetry={handleRefresh}
        />
      </PageContainer>
    );
  }

  const tools = toolsData?.tools ?? [];
  const categories = serverInfo.categories ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('mcpServer.title')}
        subtitle={t('mcpServer.subtitle')}
        actions={
          <Button variant="secondary" size="sm" onClick={handleRefresh}>
            <RefreshCw className="w-4 h-4 mr-2" />
            {t('mcpServer.refresh')}
          </Button>
        }
      />

      {/* ── Stats row ─────────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard
          title={t('mcpServer.stats.protocolVersion')}
          value={serverInfo.protocolVersion}
          icon={<Zap className="w-5 h-5" />}
        />
        <StatCard
          title={t('mcpServer.stats.serverVersion')}
          value={serverInfo.serverVersion}
          icon={<Cpu className="w-5 h-5" />}
        />
        <StatCard
          title={t('mcpServer.stats.toolCount')}
          value={String(serverInfo.toolCount)}
          icon={<Layers className="w-5 h-5" />}
        />
        <StatCard
          title={t('mcpServer.stats.categories')}
          value={String(categories.length)}
          icon={<Tag className="w-5 h-5" />}
        />
      </div>

      {/* ── Server info & connection ───────────────────────────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-6">
        <Card>
          <CardBody>
            <h3 className="text-sm font-semibold text-content-primary mb-3 flex items-center gap-2">
              <Cpu className="w-4 h-4 text-accent" />
              {t('mcpServer.serverInfo.title')}
            </h3>
            <p className="text-sm text-content-secondary mb-4">
              {serverInfo.description}
            </p>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-content-secondary">{t('mcpServer.serverInfo.name')}</span>
                <span className="text-content-primary font-mono">{serverInfo.serverName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-content-secondary">{t('mcpServer.serverInfo.protocol')}</span>
                <Badge variant="success">{serverInfo.protocolVersion}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-content-secondary">{t('mcpServer.serverInfo.capabilities')}</span>
                <div className="flex gap-1">
                  {serverInfo.capabilities.tools && (
                    <Badge variant="info">{t('mcpServer.serverInfo.capTools')}</Badge>
                  )}
                </div>
              </div>
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardBody>
            <h3 className="text-sm font-semibold text-content-primary mb-3 flex items-center gap-2">
              <Plug className="w-4 h-4 text-accent" />
              {t('mcpServer.connection.title')}
            </h3>
            <p className="text-sm text-content-secondary mb-3">
              {t('mcpServer.connection.description')}
            </p>
            <div className="flex items-center gap-2 bg-surface-elevated rounded-md p-3">
              <code className="text-xs text-accent flex-1 truncate font-mono">
                {window.location.origin}/api/v1{serverInfo.endpointUrl}
              </code>
              <button
                type="button"
                className="text-content-muted hover:text-content-primary transition-colors flex-shrink-0"
                onClick={handleCopyEndpoint}
                aria-label={t('mcpServer.connection.copyAriaLabel')}
              >
                {copiedEndpoint
                  ? <Check className="w-4 h-4 text-success" />
                  : <Copy className="w-4 h-4" />}
              </button>
            </div>
            <div className="mt-3 space-y-1 text-xs text-content-secondary">
              <p className="flex items-center gap-1">
                <span className="font-semibold text-content-primary">GET</span>
                {t('mcpServer.connection.getDescription')}
              </p>
              <p className="flex items-center gap-1">
                <span className="font-semibold text-content-primary">POST</span>
                {t('mcpServer.connection.postDescription')}
              </p>
            </div>
            <div className="mt-4">
              <a
                href="https://spec.modelcontextprotocol.io"
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1 text-xs text-accent hover:underline"
              >
                {t('mcpServer.connection.specLink')}
                <ExternalLink className="w-3 h-3" />
              </a>
            </div>
          </CardBody>
        </Card>
      </div>

      {/* ── Tool categories ───────────────────────────────────────────── */}
      {categories.length > 0 && (
        <div className="mb-4">
          <div className="flex flex-wrap gap-2">
            {categories.map(cat => (
              <Badge key={cat} variant="secondary">
                {cat}
              </Badge>
            ))}
          </div>
        </div>
      )}

      {/* ── Tools list ────────────────────────────────────────────────── */}
      <h3 className="text-sm font-semibold text-content-primary mb-3">
        {t('mcpServer.tools.title', { count: tools.length })}
      </h3>

      {tools.length === 0 ? (
        <EmptyState
          title={t('mcpServer.tools.emptyTitle')}
          description={t('mcpServer.tools.emptyMessage')}
        />
      ) : (
        <div className="space-y-2">
          {tools.map(tool => {
            const isExpanded = expandedTool === tool.name;
            const paramKeys = Object.keys(tool.inputSchema.properties ?? {});

            return (
              <Card key={tool.name}>
                <CardBody>
                  <button
                    type="button"
                    className="w-full flex items-start justify-between text-left gap-3"
                    onClick={() => setExpandedTool(isExpanded ? null : tool.name)}
                    aria-expanded={isExpanded}
                  >
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <code className="text-sm font-mono text-accent font-semibold">
                          {tool.name}
                        </code>
                        {tool.inputSchema.required.length > 0 && (
                          <Badge variant="info" size="sm">
                            {t('mcpServer.tools.requiredBadge', {
                              count: tool.inputSchema.required.length,
                            })}
                          </Badge>
                        )}
                        {paramKeys.length === 0 && (
                          <Badge variant="secondary" size="sm">
                            {t('mcpServer.tools.noParams')}
                          </Badge>
                        )}
                      </div>
                      <p className="text-sm text-content-secondary mt-1 truncate">
                        {tool.description}
                      </p>
                    </div>
                    {isExpanded
                      ? <ChevronUp className="w-4 h-4 text-content-muted flex-shrink-0 mt-0.5" />
                      : <ChevronDown className="w-4 h-4 text-content-muted flex-shrink-0 mt-0.5" />}
                  </button>

                  {isExpanded && paramKeys.length > 0 && (
                    <div className="mt-3 pt-3 border-t border-border">
                      <p className="text-xs font-semibold text-content-secondary mb-2 uppercase tracking-wide">
                        {t('mcpServer.tools.parameters')}
                      </p>
                      <div className="space-y-2">
                        {paramKeys.map(paramName => {
                          const param = tool.inputSchema.properties[paramName];
                          const isRequired = tool.inputSchema.required.includes(paramName);
                          return (
                            <div
                              key={paramName}
                              className="flex items-start gap-2 text-sm"
                            >
                              <code className="font-mono text-xs text-accent mt-0.5 min-w-[120px]">
                                {paramName}
                              </code>
                              <Badge variant={isRequired ? 'warning' : 'secondary'} size="sm">
                                {param.type}
                              </Badge>
                              {isRequired && (
                                <Badge variant="warning" size="sm">
                                  {t('mcpServer.tools.required')}
                                </Badge>
                              )}
                              <span className="text-content-secondary text-xs">
                                {param.description}
                              </span>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  )}
                </CardBody>
              </Card>
            );
          })}
        </div>
      )}
    </PageContainer>
  );
}
