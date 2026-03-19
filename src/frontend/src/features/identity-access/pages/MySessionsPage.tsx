import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Monitor, RefreshCw } from 'lucide-react';
import { Card, CardHeader } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { identityApi } from '../api';
import { useAuth } from '../../../contexts/AuthContext';
import type { ActiveSession } from '../../../types';
import { PageContainer } from '../../../components/shell';

/**
 * Página de gestão de sessões ativas do usuário autenticado.
 *
 * Exibe todas as sessões do usuário com detalhes de IP, browser/user-agent,
 * data de criação e expiração. Permite revogar sessões individuais
 * (exceto a sessão corrente, identificada pelo token ativo).
 *
 * Segue o mesmo padrão visual e de dados das páginas BreakGlass e JitAccess.
 * Todos os textos são resolvidos via i18n (chaves em sessions.* e common.*).
 */
export function MySessionsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const userId = user?.id ?? '';

  const [revokeConfirmId, setRevokeConfirmId] = useState<string | null>(null);

  /** Consulta as sessões ativas do usuário autenticado. */
  const { data: sessions, isLoading, isError } = useQuery({
    queryKey: ['my-sessions', userId],
    queryFn: () => identityApi.listActiveSessions(userId),
    enabled: !!userId,
  });

  /** Mutação para revogar uma sessão ativa. */
  const revokeMutation = useMutation({
    mutationFn: (sessionId: string) => identityApi.revoke(sessionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-sessions'] });
      setRevokeConfirmId(null);
    },
  });

  /** Determina a variante de Badge com base no status calculado da sessão. */
  const getSessionStatus = (session: ActiveSession): { label: string; variant: 'success' | 'default' | 'danger' } => {
    const now = new Date();
    const expires = new Date(session.expiresAt);
    if (expires < now) {
      return { label: t('sessions.expired'), variant: 'default' };
    }
    return { label: t('sessions.active'), variant: 'success' };
  };

  /** Extrai um nome de browser legível a partir do user-agent. */
  const parseBrowser = (ua: string): string => {
    if (!ua) return '—';
    if (ua.includes('Edg')) return 'Edge';
    if (ua.includes('OPR') || ua.includes('Opera')) return 'Opera';
    if (ua.includes('Firefox')) return 'Firefox';
    if (ua.includes('Chrome')) return 'Chrome';
    if (ua.includes('Safari')) return 'Safari';
    return ua.length > 40 ? `${ua.slice(0, 40)}…` : ua;
  };

  return (
    <PageContainer>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('sessions.title')}</h1>
          <p className="text-muted mt-1">{t('sessions.subtitle')}</p>
        </div>
      </div>

      {/* Diálogo de confirmação inline para revogação */}
      {revokeConfirmId && (
        <div className="mb-4 rounded-md bg-warning/10 border border-warning/30 px-4 py-3 flex items-center justify-between">
          <span className="text-sm text-body">{t('sessions.revokeConfirm')}</span>
          <div className="flex gap-2">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setRevokeConfirmId(null)}
            >
              {t('common.cancel')}
            </Button>
            <Button
              variant="danger"
              size="sm"
              loading={revokeMutation.isPending}
              onClick={() => revokeMutation.mutate(revokeConfirmId)}
            >
              {t('sessions.revokeSession')}
            </Button>
          </div>
        </div>
      )}

      {/* Feedback de revogação bem-sucedida */}
      {revokeMutation.isSuccess && (
        <div className="mb-4 rounded-md bg-success/10 border border-success/30 px-4 py-3 text-sm text-success">
          {t('sessions.revokedSuccess')}
        </div>
      )}

      <Card>
        <CardHeader>
          <h2 className="text-base font-semibold text-heading">{t('sessions.title')}</h2>
        </CardHeader>
        <div className="overflow-x-auto">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-muted" />
            </div>
          ) : isError ? (
            <div className="px-6 py-12 text-sm text-critical text-center">
              {t('errors.fetchFailed')}
            </div>
          ) : !sessions?.length ? (
            <EmptyState
              icon={<Monitor size={24} />}
              title={t('sessions.noSessions')}
            />
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-6 py-3 font-medium text-muted">{t('sessions.ipAddress')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('sessions.browser')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('sessions.createdAt')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('sessions.expiresAt')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('sessions.status')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {sessions.map((session) => {
                  const status = getSessionStatus(session);
                  return (
                    <tr key={session.sessionId} className="hover:bg-hover transition-colors">
                      <td className="px-6 py-3 text-body font-mono text-xs">
                        {session.ipAddress || '—'}
                      </td>
                      <td className="px-6 py-3 text-body">
                        {parseBrowser(session.userAgent)}
                      </td>
                      <td className="px-6 py-3 text-body">
                        —
                      </td>
                      <td className="px-6 py-3 text-body">
                        {new Date(session.expiresAt).toLocaleString()}
                      </td>
                      <td className="px-6 py-3">
                        <Badge variant={status.variant}>{status.label}</Badge>
                      </td>
                      <td className="px-6 py-3">
                        {status.variant === 'success' && (
                          <Button
                            variant="danger"
                            size="sm"
                            onClick={() => setRevokeConfirmId(session.sessionId)}
                            loading={revokeMutation.isPending && revokeConfirmId === session.sessionId}
                          >
                            {t('sessions.revokeSession')}
                          </Button>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </Card>
    </PageContainer>
  );
}
