import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { KeyRound, Plus, Copy, Trash2, Check } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

interface ApiKeySummary {
  apiKeyId: string;
  name: string;
  scopes: string[];
  expiresAt?: string;
  createdAt: string;
  lastUsedAt?: string;
}

interface CreateApiKeyResponse {
  apiKeyId: string;
  name: string;
  rawKey: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────────

const useApiKeys = () =>
  useQuery({
    queryKey: ['api-keys'],
    queryFn: () =>
      client
        .get<{ items: ApiKeySummary[] }>('/api/v1/developerportal/api-keys')
        .then(r => r.data),
  });

const useCreateApiKey = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { name: string; scopes: string[]; expiresAt?: string }) =>
      client.post<CreateApiKeyResponse>('/api/v1/developerportal/api-keys', data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['api-keys'] }),
  });
};

const useRevokeApiKey = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (apiKeyId: string) => client.delete(`/api/v1/developerportal/api-keys/${apiKeyId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['api-keys'] }),
  });
};

// ── Constants ──────────────────────────────────────────────────────────────────

const AVAILABLE_SCOPES = ['read', 'write', 'contracts:read', 'changes:read', 'services:read', 'ai:use'];

// ── Component ──────────────────────────────────────────────────────────────────

/**
 * APIKeysPage — gestão de chaves de API do utilizador/tenant.
 * Permite criar chaves com scopes específicos e revogar chaves existentes.
 * Pilar: Platform Customization — Integrations & API
 */
export function APIKeysPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useApiKeys();
  const createApiKey = useCreateApiKey();
  const revokeApiKey = useRevokeApiKey();

  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [scopes, setScopes] = useState<string[]>(['read']);
  const [expiresAt, setExpiresAt] = useState('');
  const [newKey, setNewKey] = useState<CreateApiKeyResponse | null>(null);
  const [copied, setCopied] = useState(false);

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const items = data?.items ?? [];

  const handleCreate = () => {
    if (!name.trim()) return;
    createApiKey.mutate(
      { name, scopes, expiresAt: expiresAt || undefined },
      {
        onSuccess: (result) => {
          setNewKey(result);
          setShowForm(false);
          setName('');
          setScopes(['read']);
          setExpiresAt('');
        },
      }
    );
  };

  const handleCopy = () => {
    if (newKey) {
      navigator.clipboard.writeText(newKey.rawKey).then(() => {
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      });
    }
  };

  const toggleScope = (scope: string) => {
    setScopes(prev =>
      prev.includes(scope) ? prev.filter(s => s !== scope) : [...prev, scope]
    );
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('apiKeys.title')}
        actions={
          <Button variant="primary" onClick={() => setShowForm(s => !s)}>
            <Plus size={16} className="mr-1" />
            {t('apiKeys.create')}
          </Button>
        }
      />

      {/* Newly created key — show once */}
      {newKey && (
        <Card className="mb-6 border-success">
          <CardBody>
            <p className="text-sm font-medium text-success mb-2">{t('apiKeys.copyOnce')}</p>
            <div className="flex items-center gap-2">
              <code className="flex-1 text-xs bg-muted px-3 py-2 rounded font-mono break-all">{newKey.rawKey}</code>
              <button
                type="button"
                onClick={handleCopy}
                className="p-2 rounded hover:bg-muted transition-colors"
                title={t('common.copy', 'Copy')}
              >
                {copied ? <Check size={16} className="text-success" /> : <Copy size={16} />}
              </button>
            </div>
            <button
              type="button"
              onClick={() => setNewKey(null)}
              className="mt-3 text-xs text-muted-foreground hover:underline"
            >
              {t('common.dismiss', 'Dismiss')}
            </button>
          </CardBody>
        </Card>
      )}

      {/* Create form */}
      {showForm && (
        <Card className="mb-6">
          <CardBody>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">{t('common.name', 'Name')}</label>
                <input
                  type="text"
                  value={name}
                  onChange={e => setName(e.target.value)}
                  className="w-full px-3 py-1.5 text-sm border rounded bg-transparent"
                  placeholder={t('common.name', 'Name')}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2">{t('apiKeys.scopes')}</label>
                <div className="flex flex-wrap gap-2">
                  {AVAILABLE_SCOPES.map(scope => (
                    <button
                      key={scope}
                      type="button"
                      onClick={() => toggleScope(scope)}
                      className={`text-xs px-3 py-1.5 rounded-full border transition-colors ${
                        scopes.includes(scope)
                          ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 font-medium'
                          : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-blue-300'
                      }`}
                    >
                      {scope}
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">{t('apiKeys.expires')}</label>
                <input
                  type="date"
                  value={expiresAt}
                  onChange={e => setExpiresAt(e.target.value)}
                  className="w-48 px-3 py-1.5 text-sm border rounded bg-transparent"
                />
              </div>
              <div className="flex gap-2">
                <Button
                  variant="primary"
                  onClick={handleCreate}
                  disabled={createApiKey.isPending}
                >
                  {t('apiKeys.create')}
                </Button>
                <Button variant="ghost" onClick={() => setShowForm(false)}>
                  {t('common.cancel', 'Cancel')}
                </Button>
              </div>
            </div>
          </CardBody>
        </Card>
      )}

      {items.length === 0 ? (
        <EmptyState
          icon={<KeyRound size={32} />}
          title={t('apiKeys.empty')}
        />
      ) : (
        <Card>
          <CardBody className="p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left">
                  <th className="px-4 py-3 font-medium">{t('common.name', 'Name')}</th>
                  <th className="px-4 py-3 font-medium">{t('apiKeys.scopes')}</th>
                  <th className="px-4 py-3 font-medium">{t('apiKeys.expires')}</th>
                  <th className="px-4 py-3 font-medium w-16">{t('common.actions', 'Actions')}</th>
                </tr>
              </thead>
              <tbody>
                {items.map(item => (
                  <tr key={item.apiKeyId} className="border-b last:border-0 hover:bg-muted/30 transition-colors">
                    <td className="px-4 py-3 font-medium">{item.name}</td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {item.scopes.map(s => (
                          <Badge key={s} variant="info" className="text-xs">{s}</Badge>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground text-xs">
                      {item.expiresAt ? new Date(item.expiresAt).toLocaleDateString() : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <button
                        type="button"
                        title={t('apiKeys.revoke')}
                        onClick={() => revokeApiKey.mutate(item.apiKeyId)}
                        className="p-1 rounded hover:bg-muted text-muted-foreground hover:text-critical transition-colors"
                      >
                        <Trash2 size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}

export default APIKeysPage;
