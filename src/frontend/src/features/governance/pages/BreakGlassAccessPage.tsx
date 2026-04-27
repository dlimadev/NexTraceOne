import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ShieldAlert, Clock, CheckCircle2, XCircle, AlertTriangle, Key } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import client from '../../../api/client';

interface JitRequest {
  id: string;
  requestedBy: string;
  resource: string;
  role: string;
  justification: string;
  requestedAt: string;
  expiresAt?: string;
  status: 'Pending' | 'Approved' | 'Denied' | 'Expired' | 'Active';
  approvedBy?: string;
  environment: string;
}

const useJitRequests = () =>
  useQuery({
    queryKey: ['break-glass-access'],
    queryFn: () =>
      client
        .get<{ requests: JitRequest[]; isSimulated: boolean }>(
          '/api/v1/platform/jit-access/requests',
          { params: { tenantId: 'default' } }
        )
        .then((r) => r.data),
  });

const STATUS_CONFIG = {
  Pending: { badge: 'warning' as const, icon: <Clock size={12} /> },
  Approved: { badge: 'success' as const, icon: <CheckCircle2 size={12} /> },
  Active: { badge: 'info' as const, icon: <Key size={12} /> },
  Denied: { badge: 'destructive' as const, icon: <XCircle size={12} /> },
  Expired: { badge: 'secondary' as const, icon: <Clock size={12} /> },
};

export function BreakGlassAccessPage() {
  const { t } = useTranslation();
  const [justification, setJustification] = useState('');
  const [resource, setResource] = useState('');
  const [role, setRole] = useState('');
  const [env, setEnv] = useState('production');
  const qc = useQueryClient();
  const { data, isLoading } = useJitRequests();

  const requestAccess = useMutation({
    mutationFn: () =>
      client.post('/api/v1/platform/jit-access/request', {
        tenantId: 'default',
        userId: 'current-user',
        resource,
        role,
        justification,
        environment: env,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['break-glass-access'] });
      setJustification('');
      setResource('');
      setRole('');
    },
  });

  const approve = useMutation({
    mutationFn: (id: string) =>
      client.post(`/api/v1/platform/jit-access/requests/${id}/approve`, { tenantId: 'default', userId: 'current-user' }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['break-glass-access'] }),
  });

  const deny = useMutation({
    mutationFn: (id: string) =>
      client.post(`/api/v1/platform/jit-access/requests/${id}/deny`, { tenantId: 'default', userId: 'current-user' }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['break-glass-access'] }),
  });

  const pending = (data?.requests ?? []).filter((r) => r.status === 'Pending');
  const active = (data?.requests ?? []).filter((r) => r.status === 'Active');
  const history = (data?.requests ?? []).filter((r) => ['Approved', 'Denied', 'Expired'].includes(r.status));

  return (
    <PageContainer>
      <PageHeader
        title={t('breakGlass.title')}
        subtitle={t('breakGlass.subtitle')}
      />
      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          {/* Request form */}
          <div className="lg:col-span-1">
            <Card>
              <CardBody className="p-4">
                <div className="flex items-center gap-2 mb-4">
                  <ShieldAlert size={14} className="text-destructive" />
                  <h3 className="text-sm font-semibold">{t('breakGlass.requestAccess')}</h3>
                </div>
                <div className="space-y-3">
                  <div>
                    <label className="text-xs text-muted-foreground">{t('breakGlass.resource')}</label>
                    <input value={resource} onChange={(e) => setResource(e.target.value)} placeholder="e.g. prod-database" className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background" />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">{t('breakGlass.role')}</label>
                    <select value={role} onChange={(e) => setRole(e.target.value)} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background">
                      <option value="">-- select --</option>
                      <option value="readonly">Read Only</option>
                      <option value="admin">Admin</option>
                      <option value="break-glass">Break Glass</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">{t('breakGlass.environment')}</label>
                    <select value={env} onChange={(e) => setEnv(e.target.value)} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background">
                      <option value="production">Production</option>
                      <option value="staging">Staging</option>
                      <option value="development">Development</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">{t('breakGlass.justification')}</label>
                    <textarea
                      value={justification}
                      onChange={(e) => setJustification(e.target.value)}
                      rows={3}
                      placeholder={t('breakGlass.justificationPlaceholder')}
                      className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background"
                    />
                  </div>
                  <Button
                    className="w-full"
                    disabled={!resource || !role || !justification || requestAccess.isPending}
                    onClick={() => requestAccess.mutate()}
                  >
                    <ShieldAlert size={14} className="mr-1" />
                    {t('breakGlass.submit')}
                  </Button>
                </div>
              </CardBody>
            </Card>
          </div>

          {/* Requests list */}
          <div className="lg:col-span-2 space-y-4">
            {isLoading ? (
              <PageLoadingState />
            ) : (
              <>
                {/* Active sessions */}
                {active.length > 0 && (
                  <div>
                    <h3 className="text-sm font-semibold mb-2 flex items-center gap-2">
                      <Key size={12} className="text-info" />
                      {t('breakGlass.activeSessions')} ({active.length})
                    </h3>
                    <div className="space-y-2">
                      {active.map((req) => (
                        <Card key={req.id} className="border-info/40">
                          <CardBody className="p-3">
                            <div className="flex items-center justify-between gap-2">
                              <div>
                                <p className="text-sm font-medium">{req.resource} — <span className="font-mono text-xs">{req.role}</span></p>
                                <p className="text-xs text-muted-foreground">{req.requestedBy} · {req.environment}</p>
                                {req.expiresAt && <p className="text-xs text-warning">{t('breakGlass.expires')}: {new Date(req.expiresAt).toLocaleString()}</p>}
                              </div>
                              <Badge variant="info">{t('breakGlass.active')}</Badge>
                            </div>
                          </CardBody>
                        </Card>
                      ))}
                    </div>
                  </div>
                )}

                {/* Pending approvals */}
                <div>
                  <h3 className="text-sm font-semibold mb-2 flex items-center gap-2">
                    <AlertTriangle size={12} className="text-warning" />
                    {t('breakGlass.pendingApprovals')} ({pending.length})
                  </h3>
                  <div className="space-y-2">
                    {pending.map((req) => (
                      <Card key={req.id}>
                        <CardBody className="p-3">
                          <div className="flex items-start justify-between gap-3">
                            <div className="flex-1 min-w-0">
                              <div className="flex items-center gap-2 mb-1">
                                <span className="text-sm font-medium">{req.requestedBy}</span>
                                <Badge variant="outline" className="text-xs">{req.environment}</Badge>
                              </div>
                              <p className="text-xs font-mono text-muted-foreground">{req.resource} → {req.role}</p>
                              <p className="text-xs text-muted-foreground mt-1 italic">{req.justification}</p>
                            </div>
                            <div className="flex gap-1">
                              <Button size="sm" variant="ghost" className="text-success" onClick={() => approve.mutate(req.id)} disabled={approve.isPending}>
                                <CheckCircle2 size={12} />
                              </Button>
                              <Button size="sm" variant="ghost" className="text-destructive" onClick={() => deny.mutate(req.id)} disabled={deny.isPending}>
                                <XCircle size={12} />
                              </Button>
                            </div>
                          </div>
                        </CardBody>
                      </Card>
                    ))}
                    {pending.length === 0 && (
                      <div className="text-center p-4 text-muted-foreground text-sm">{t('breakGlass.noPending')}</div>
                    )}
                  </div>
                </div>

                {/* History */}
                {history.length > 0 && (
                  <div>
                    <h3 className="text-sm font-semibold mb-2">{t('breakGlass.history')}</h3>
                    <div className="space-y-1">
                      {history.map((req) => {
                        const cfg = STATUS_CONFIG[req.status] ?? STATUS_CONFIG.Expired;
                        return (
                          <Card key={req.id}>
                            <CardBody className="p-2.5 flex items-center justify-between gap-3">
                              <div>
                                <p className="text-xs font-medium">{req.requestedBy} — {req.resource}</p>
                                <p className="text-xs text-muted-foreground">{new Date(req.requestedAt).toLocaleString()}</p>
                              </div>
                              <Badge variant={cfg.badge} className="flex items-center gap-1 text-xs">
                                {cfg.icon}
                                {req.status}
                              </Badge>
                            </CardBody>
                          </Card>
                        );
                      })}
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        </div>

        <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
          {t('sotCenter.simulatedBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
