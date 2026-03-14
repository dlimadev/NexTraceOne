import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Plus, RefreshCw } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { identityApi } from '../api';
import { useAuth } from '../../../contexts/AuthContext';

/**
 * Página de gestão de usuários do módulo Identity.
 * Lista usuários do tenant atual com paginação e permite criação de novos usuários.
 * Todos os textos são resolvidos via i18n (chaves em users.*).
 *
 * O tenantId é obtido do AuthContext (fonte de verdade) em vez de localStorage,
 * evitando manipulação direta de storage e garantindo consistência com o estado de autenticação.
 */
export function UsersPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { tenantId: authTenantId } = useAuth();
  const tenantId = authTenantId ?? '';
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ email: '', firstName: '', lastName: '', tenantId });

  const { data, isLoading } = useQuery({
    queryKey: ['users', tenantId],
    queryFn: () => identityApi.listTenantUsers(tenantId, 1, 50),
    enabled: !!tenantId,
  });

  const createMutation = useMutation({
    mutationFn: identityApi.createUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setShowForm(false);
      setForm({ email: '', firstName: '', lastName: '', tenantId });
    },
  });

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('users.title')}</h1>
          <p className="text-muted mt-1">{t('users.subtitle')}</p>
        </div>
        <Button onClick={() => setShowForm((v) => !v)}>
          <Plus size={16} /> {t('users.createUser')}
        </Button>
      </div>

      {/* Formulário de criação de usuário */}
      {showForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-heading">{t('users.createNewUser')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); createMutation.mutate(form); }}
              className="grid grid-cols-3 gap-4"
            >
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('users.firstName')}</label>
                <input
                  type="text"
                  value={form.firstName}
                  onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))}
                  required
                  maxLength={100}
                  autoComplete="given-name"
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('users.lastName')}</label>
                <input
                  type="text"
                  value={form.lastName}
                  onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))}
                  required
                  maxLength={100}
                  autoComplete="family-name"
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('users.email')}</label>
                <input
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                  required
                  maxLength={254}
                  autoComplete="email"
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                />
              </div>
              <div className="col-span-3 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={createMutation.isPending}>{t('common.create')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Tabela de usuários do tenant */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h2 className="text-base font-semibold text-heading">{t('users.tenantUsers')}</h2>
            {data && <span className="text-sm text-muted">{data.totalCount} {t('common.total')}</span>}
          </div>
        </CardHeader>
        <div className="overflow-x-auto">
          {!tenantId ? (
            <p className="px-6 py-12 text-sm text-muted text-center">{t('users.noTenantId')}</p>
          ) : isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-muted" />
            </div>
          ) : !data?.items?.length ? (
            <p className="px-6 py-12 text-sm text-muted text-center">{t('users.noUsersFound')}</p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-6 py-3 font-medium text-muted">{t('users.name')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('users.email')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('users.roles')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {data.items.map((u) => (
                  <tr key={u.userId} className="hover:bg-hover transition-colors">
                    <td className="px-6 py-3">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-accent/15 flex items-center justify-center text-accent font-medium text-sm">
                          {u.email[0].toUpperCase()}
                        </div>
                        <span className="font-medium text-heading">{u.fullName}</span>
                      </div>
                    </td>
                    <td className="px-6 py-3 text-body">{u.email}</td>
                    <td className="px-6 py-3">
                      <Badge variant="info">{u.roleName}</Badge>
                    </td>
                    <td className="px-6 py-3">
                      <Badge variant={u.isActive ? 'success' : 'default'}>
                        {u.isActive ? t('users.active') : t('users.inactive')}
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </Card>
    </div>
  );
}
