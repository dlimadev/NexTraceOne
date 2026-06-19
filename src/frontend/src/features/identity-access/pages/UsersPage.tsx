import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Plus, RefreshCw, Users } from 'lucide-react';
import { Card } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { StatCard } from '../../../components/StatCard';
import { DataTable } from '../../../components/DataTable';
import type { DataTableColumn } from '../../../components/DataTable';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { Drawer } from '../../../components/Drawer';
import { identityApi } from '../api';
import { useAuth } from '../../../contexts/AuthContext';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

/**
 * Página de gestão de usuários do módulo Identity.
 * Lista usuários do tenant atual com paginação e permite criação de novos usuários.
 * Todos os textos são resolvidos via i18n (chaves em users.*).
 *
 * O tenantId é obtido do AuthContext (fonte de verdade) em vez de localStorage,
 * evitando manipulação direta de storage e garantindo consistência com o estado de autenticação.
 *
 * Redesenho Betterstack: Drawer para formulário, DataTable para listagem, StatCard para KPI.
 */

/** Tipo de linha da tabela de usuários */
interface UserRow {
  userId: string;
  email: string;
  fullName: string;
  roleName: string;
  isActive: boolean;
}

export function UsersPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { tenantId: authTenantId } = useAuth();
  const tenantId = authTenantId ?? '';
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [form, setForm] = useState({ email: '', firstName: '', lastName: '', tenantId, roleId: '' });

  const { data: rolesData } = useQuery({
    queryKey: ['roles'],
    queryFn: () => identityApi.listRoles(),
  });

  const { data, isLoading, isError: isUsersError, refetch: refetchUsers } = useQuery({
    queryKey: ['users', tenantId],
    queryFn: () => identityApi.listTenantUsers(tenantId, 1, 50),
    enabled: !!tenantId,
  });

  const createMutation = useMutation({
    mutationFn: identityApi.createUser,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      setDrawerOpen(false);
      setForm({ email: '', firstName: '', lastName: '', tenantId, roleId: '' });
    },
  });

  /** Opções de roles para o Select do DS */
  const roleOptions = (Array.isArray(rolesData) ? rolesData : (rolesData as { items?: { id: string; name: string }[] } | undefined)?.items ?? []).map(
    (r: { id: string; name: string }) => ({ value: r.id, label: r.name }),
  );

  /** Colunas tipadas para o DataTable */
  const columns: DataTableColumn<UserRow>[] = [
    {
      id: 'name',
      header: t('users.name', 'Name'),
      sortable: true,
      sortValue: (u) => u.fullName,
      accessor: (u) => (
        <div className="flex items-center gap-3">
          {/* Avatar inicial */}
          <div
            className="w-8 h-8 rounded-full bg-accent/15 flex items-center justify-center text-accent font-medium text-sm shrink-0"
            aria-hidden="true"
          >
            {(u.email.charAt(0) || 'U').toUpperCase()}
          </div>
          <span className="font-medium text-heading">{u.fullName}</span>
        </div>
      ),
    },
    {
      id: 'email',
      header: t('users.email', 'Email'),
      sortable: true,
      sortValue: (u) => u.email,
      accessor: (u) => <span className="text-body">{u.email}</span>,
    },
    {
      id: 'role',
      header: t('users.roles', 'Role'),
      accessor: (u) => <Badge variant="info">{u.roleName}</Badge>,
    },
    {
      id: 'status',
      header: t('users.status', 'Status'),
      accessor: (u) => (
        <Badge variant={u.isActive ? 'success' : 'default'}>
          {u.isActive ? t('users.active', 'Active') : t('users.inactive', 'Inactive')}
        </Badge>
      ),
    },
  ];

  const users: UserRow[] = (data?.items ?? []) as UserRow[];
  const totalCount = data?.totalCount ?? 0;

  return (
    <PageContainer>
      {/* Cabeçalho com CTA principal no header */}
      <PageHeader
        title={t('users.title', 'Users')}
        subtitle={t('users.subtitle', 'Manage tenant members and their access roles.')}
        actions={
          <>
            <Button
              variant="secondary"
              size="md"
              icon={<RefreshCw size={16} />}
              onClick={() => void refetchUsers()}
              aria-label={t('common.refresh', 'Refresh')}
            >
              {t('common.refresh', 'Refresh')}
            </Button>
            <Button
              variant="primary"
              size="md"
              icon={<Plus size={16} />}
              onClick={() => setDrawerOpen(true)}
            >
              {t('users.createUser', 'Create User')}
            </Button>
          </>
        }
      />

      {/* KPI de total de usuários */}
      {!!tenantId && !isLoading && !isUsersError && (
        <PageSection>
          <StatsGrid columns={4}>
            <StatCard
              title={t('users.totalUsers', 'Total Users')}
              value={totalCount}
              icon={<Users size={18} />}
              color="text-accent"
            />
          </StatsGrid>
        </PageSection>
      )}

      {/* Tabela de usuários do tenant */}
      <PageSection>
        {!tenantId ? (
          <Card>
            <p className="px-6 py-12 text-sm text-muted text-center">{t('users.noTenantId', 'No tenant selected.')}</p>
          </Card>
        ) : isLoading ? (
          <PageLoadingState />
        ) : isUsersError ? (
          <PageErrorState
            action={
              <Button
                variant="secondary"
                size="sm"
                icon={<RefreshCw size={14} />}
                onClick={() => void refetchUsers()}
              >
                {t('common.retry', 'Retry')}
              </Button>
            }
          />
        ) : (
          <DataTable<UserRow>
            columns={columns}
            data={users}
            rowKey={(u) => u.userId}
            emptyTitle={t('users.noUsersFound', 'No users found')}
            emptyAction={
              <Button
                variant="primary"
                size="sm"
                icon={<Plus size={16} />}
                onClick={() => setDrawerOpen(true)}
              >
                {t('users.createUser', 'Create User')}
              </Button>
            }
          />
        )}
      </PageSection>

      {/* Drawer de criação de usuário — substitui o inline form */}
      <Drawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={t('users.createNewUser', 'Create New User')}
        size="md"
        footer={
          <>
            <Button
              variant="secondary"
              type="button"
              onClick={() => setDrawerOpen(false)}
            >
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="primary"
              type="submit"
              form="create-user-form"
              loading={createMutation.isPending}
            >
              {t('common.create', 'Create')}
            </Button>
          </>
        }
      >
        <form
          id="create-user-form"
          onSubmit={(e) => {
            e.preventDefault();
            createMutation.mutate(form);
          }}
          className="flex flex-col gap-4"
        >
          <TextField
            id="user-firstName"
            label={t('users.firstName', 'First Name')}
            type="text"
            value={form.firstName}
            onChange={(e) => setForm((f) => ({ ...f, firstName: e.target.value }))}
            required
            maxLength={100}
            autoComplete="given-name"
          />
          <TextField
            id="user-lastName"
            label={t('users.lastName', 'Last Name')}
            type="text"
            value={form.lastName}
            onChange={(e) => setForm((f) => ({ ...f, lastName: e.target.value }))}
            required
            maxLength={100}
            autoComplete="family-name"
          />
          <TextField
            id="user-email"
            label={t('users.email', 'Email')}
            type="email"
            value={form.email}
            onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
            required
            maxLength={254}
            autoComplete="email"
          />
          <Select
            id="user-role"
            label={t('users.role', 'Role')}
            options={roleOptions}
            placeholder={t('users.selectRole', 'Select a role')}
            value={form.roleId}
            onChange={(e) => setForm((f) => ({ ...f, roleId: e.target.value }))}
            required
          />
        </form>
      </Drawer>
    </PageContainer>
  );
}
