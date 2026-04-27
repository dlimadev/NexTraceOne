import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Globe, Plus, Trash2, ChevronDown, ChevronRight, Upload } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

interface Endpoint {
  id: string;
  method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  path: string;
  summary: string;
  tags: string[];
}

const METHOD_COLORS: Record<string, string> = {
  GET: 'text-success',
  POST: 'text-accent',
  PUT: 'text-warning',
  PATCH: 'text-info',
  DELETE: 'text-destructive',
};

const METHOD_BADGE: Record<string, 'success' | 'default' | 'warning' | 'info' | 'destructive'> = {
  GET: 'success',
  POST: 'default',
  PUT: 'warning',
  PATCH: 'info',
  DELETE: 'destructive',
};

export function RestOpenApiBuilderPage() {
  const { t } = useTranslation();
  const [title, setTitle] = useState('My REST API');
  const [version, setVersion] = useState('1.0.0');
  const [baseUrl, setBaseUrl] = useState('https://api.example.com/v1');
  const [endpoints, setEndpoints] = useState<Endpoint[]>([
    { id: '1', method: 'GET', path: '/resources', summary: 'List resources', tags: ['resources'] },
    { id: '2', method: 'POST', path: '/resources', summary: 'Create resource', tags: ['resources'] },
    { id: '3', method: 'GET', path: '/resources/{id}', summary: 'Get resource by ID', tags: ['resources'] },
  ]);
  const [expanded, setExpanded] = useState<string | null>(null);

  const addEndpoint = () => {
    const id = Date.now().toString();
    setEndpoints((prev) => [...prev, { id, method: 'GET', path: '/new-endpoint', summary: '', tags: [] }]);
    setExpanded(id);
  };

  const removeEndpoint = (id: string) => setEndpoints((prev) => prev.filter((e) => e.id !== id));

  const updateEndpoint = (id: string, patch: Partial<Endpoint>) =>
    setEndpoints((prev) => prev.map((e) => (e.id === id ? { ...e, ...patch } : e)));

  return (
    <PageContainer>
      <PageHeader
        title={t('restBuilder.title')}
        subtitle={t('restBuilder.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button size="sm" variant="ghost">
              <Upload size={14} className="mr-1" />
              {t('restBuilder.importSpec')}
            </Button>
            <Button size="sm">{t('restBuilder.publish')}</Button>
          </div>
        }
      />
      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          {/* API Info */}
          <div className="lg:col-span-1 space-y-3">
            <Card>
              <CardBody className="p-4">
                <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-3">
                  {t('restBuilder.apiInfo')}
                </h3>
                <div className="space-y-2">
                  <div>
                    <label className="text-xs text-muted-foreground">{t('restBuilder.title')}</label>
                    <input
                      value={title}
                      onChange={(e) => setTitle(e.target.value)}
                      className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background"
                    />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">{t('restBuilder.version')}</label>
                    <input
                      value={version}
                      onChange={(e) => setVersion(e.target.value)}
                      className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background font-mono"
                    />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">{t('restBuilder.baseUrl')}</label>
                    <input
                      value={baseUrl}
                      onChange={(e) => setBaseUrl(e.target.value)}
                      className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background font-mono"
                    />
                  </div>
                </div>
              </CardBody>
            </Card>

            <Card>
              <CardBody className="p-4">
                <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">
                  {t('restBuilder.summary')}
                </h3>
                <div className="space-y-1 text-xs">
                  {Object.entries(METHOD_COLORS).map(([method, color]) => {
                    const count = endpoints.filter((e) => e.method === method).length;
                    if (!count) return null;
                    return (
                      <div key={method} className="flex items-center justify-between">
                        <span className={`font-mono font-bold ${color}`}>{method}</span>
                        <span className="text-muted-foreground">{count}</span>
                      </div>
                    );
                  })}
                  <div className="border-t pt-1 mt-1 flex items-center justify-between font-medium">
                    <span>{t('restBuilder.total')}</span>
                    <span>{endpoints.length}</span>
                  </div>
                </div>
              </CardBody>
            </Card>
          </div>

          {/* Endpoints */}
          <div className="lg:col-span-2">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <Globe size={14} className="text-accent" />
                <h3 className="text-sm font-semibold">{t('restBuilder.endpoints')}</h3>
                <Badge variant="secondary">{endpoints.length}</Badge>
              </div>
              <Button size="sm" variant="ghost" onClick={addEndpoint}>
                <Plus size={12} className="mr-1" />
                {t('restBuilder.addEndpoint')}
              </Button>
            </div>

            <div className="space-y-2">
              {endpoints.map((ep) => (
                <Card key={ep.id} className="overflow-hidden">
                  <CardBody className="p-0">
                    <button
                      className="w-full flex items-center gap-3 p-3 text-left hover:bg-muted/30 transition-colors"
                      onClick={() => setExpanded(expanded === ep.id ? null : ep.id)}
                    >
                      {expanded === ep.id ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
                      <Badge variant={METHOD_BADGE[ep.method] ?? 'default'} className="text-xs font-mono w-16 justify-center">
                        {ep.method}
                      </Badge>
                      <span className="text-sm font-mono flex-1">{ep.path}</span>
                      <span className="text-xs text-muted-foreground">{ep.summary}</span>
                    </button>

                    {expanded === ep.id && (
                      <div className="border-t p-3 space-y-2 bg-muted/20">
                        <div className="grid grid-cols-2 gap-2">
                          <div>
                            <label className="text-xs text-muted-foreground">{t('restBuilder.method')}</label>
                            <select
                              value={ep.method}
                              onChange={(e) => updateEndpoint(ep.id, { method: e.target.value as Endpoint['method'] })}
                              className="w-full mt-1 px-2 py-1.5 text-xs border rounded bg-background"
                            >
                              {['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map((m) => (
                                <option key={m} value={m}>{m}</option>
                              ))}
                            </select>
                          </div>
                          <div>
                            <label className="text-xs text-muted-foreground">{t('restBuilder.path')}</label>
                            <input
                              value={ep.path}
                              onChange={(e) => updateEndpoint(ep.id, { path: e.target.value })}
                              className="w-full mt-1 px-2 py-1.5 text-xs border rounded bg-background font-mono"
                            />
                          </div>
                        </div>
                        <div>
                          <label className="text-xs text-muted-foreground">{t('restBuilder.summary')}</label>
                          <input
                            value={ep.summary}
                            onChange={(e) => updateEndpoint(ep.id, { summary: e.target.value })}
                            className="w-full mt-1 px-2 py-1.5 text-xs border rounded bg-background"
                          />
                        </div>
                        <div className="flex justify-end">
                          <Button size="sm" variant="ghost" className="text-destructive" onClick={() => removeEndpoint(ep.id)}>
                            <Trash2 size={12} className="mr-1" />
                            {t('restBuilder.remove')}
                          </Button>
                        </div>
                      </div>
                    )}
                  </CardBody>
                </Card>
              ))}
            </div>
          </div>
        </div>

        <div className="mt-4 p-3 rounded-lg border border-accent/30 bg-accent/5 text-xs text-muted-foreground">
          {t('restBuilder.openApiBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
