import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Plus, RefreshCw, Building2, ShieldOff, ShieldCheck, Pencil } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { identityApi } from '../api';
import type { TenantAdminItem, CreateTenantRequest, UpdateTenantRequest } from '../api/identity';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import type { PagedList } from '../../../types';

const TENANT_TYPE_OPTIONS = ['Organization', 'Holding', 'Subsidiary', 'Department', 'Partner'];

interface TenantForm {
  name: string;
  slug: string;
  tenantType: string;
  legalName: string;
  taxId: string;
  parentTenantId: string;
}

const DEFAULT_FORM: TenantForm = {
  name: '',
  slug: '',
  tenantType: 'Organization',
  legalName: '',
  taxId: '',
  parentTenantId: '',
};

/**
 * Página de gestão administrativa de tenants (Platform Admin).
 *
 * Permite criar, editar, ativar e desativar tenants.
 * Cada tenant representa uma organização isolada na plataforma NexTraceOne.
 * Acessível exclusivamente para utilizadores com permissão identity:tenants:admin.
 */
export function TenantsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<TenantForm>(DEFAULT_FORM);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [filterActive, setFilterActive] = useState<boolean | undefined>(undefined);

  const {
    data,
    isLoading,
    isError,
    refetch,
  } = useQuery<PagedList<TenantAdminItem>>({
    queryKey: ['admin-tenants', search, filterActive],
    queryFn: () => identityApi.listTenantsAdmin({
      search: search || undefined,
      isActive: filterActive,
      page: 1,
      pageSize: 50,
    }),
    staleTime: 30_000,
  });

  const createMutation = useMutation({
    mutationFn: (payload: CreateTenantRequest) => identityApi.createTenantAdmin(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-tenants'] });
      setShowForm(false);
      setForm(DEFAULT_FORM);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTenantRequest }) =>
      identityApi.updateTenantAdmin(id, payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-tenants'] });
      setShowForm(false);
      setEditingId(null);
      setForm(DEFAULT_FORM);
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (tenantId: string) => identityApi.deactivateTenantAdmin(tenantId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-tenants'] });
    },
  });

  const activateMutation = useMutation({
    mutationFn: (tenantId: string) => identityApi.activateTenantAdmin(tenantId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-tenants'] });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingId) {
      const payload: UpdateTenantRequest = {
        name: form.name,
        legalName: form.legalName || undefined,
        taxId: form.taxId || undefined,
      };
      updateMutation.mutate({ id: editingId, payload });
    } else {
      const payload: CreateTenantRequest = {
        name: form.name,
        slug: form.slug.toLowerCase(),
        tenantType: form.tenantType,
        legalName: form.legalName || undefined,
        taxId: form.taxId || undefined,
        parentTenantId: form.parentTenantId || undefined,
      };
      createMutation.mutate(payload);
    }
  };

  const handleEdit = (tenant: TenantAdminItem) => {
    setForm({
      name: tenant.name,
      slug: tenant.slug,
      tenantType: tenant.tenantType,
      legalName: tenant.legalName ?? '',
      taxId: tenant.taxId ?? '',
      parentTenantId: tenant.parentTenantId ?? '',
    });
    setEditingId(tenant.id);
    setShowForm(true);
  };

  const handleDeactivate = (tenant: TenantAdminItem) => {
    if (window.confirm(t('tenants.admin.confirmDeactivate', { name: tenant.name }))) {
      deactivateMutation.mutate(tenant.id);
    }
  };

  const handleActivate = (tenant: TenantAdminItem) => {
    if (window.confirm(t('tenants.admin.confirmActivate', { name: tenant.name }))) {
      activateMutation.mutate(tenant.id);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setSearch(searchInput);
  };

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState onRetry={refetch} />;

  const tenants = data?.items ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('tenants.admin.title')}
        subtitle={t('tenants.admin.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button type="button" onClick={() => void refetch()}>
              <RefreshCw size={16} />
            </Button>
            <Button
              type="button"
              onClick={() => {
                setForm(DEFAULT_FORM);
                setEditingId(null);
                setShowForm((v) => !v);
              }}
            >
              <Plus size={16} /> {t('tenants.admin.createTenant')}
            </Button>
          </div>
        }
      />

      <PageSection>
        {/* Search & filters */}
        <div className="flex flex-wrap gap-3 mb-4">
          <form onSubmit={handleSearch} className="flex gap-2 flex-1 min-w-[240px]">
            <input
              type="text"
              placeholder={t('tenants.admin.searchPlaceholder')}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="flex-1 px-3 py-1.5 rounded border border-edge text-sm"
            />
            <Button type="submit">{t('common.search')}</Button>
          </form>
          <select
            value={filterActive === undefined ? '' : String(filterActive)}
            onChange={(e) => {
              const v = e.target.value;
              setFilterActive(v === '' ? undefined : v === 'true');
            }}
            className="px-3 py-1.5 rounded border border-edge text-sm"
          >
            <option value="">{t('tenants.admin.filterAll')}</option>
            <option value="true">{t('tenants.admin.filterActive')}</option>
            <option value="false">{t('tenants.admin.filterInactive')}</option>
          </select>
        </div>

        {/* Create/Edit Form */}
        {showForm && (
          <Card className="mb-4">
            <CardHeader>
              <h3 className="font-semibold">
                {editingId ? t('tenants.admin.editTenant') : t('tenants.admin.createNewTenant')}
              </h3>
            </CardHeader>
            <CardBody>
              <form onSubmit={handleSubmit}>
                <div className="grid gap-3">
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label htmlFor="tenant-name" className="block text-sm mb-1">
                        {t('tenants.admin.name')} *
                      </label>
                      <input
                        id="tenant-name"
                        type="text"
                        required
                        maxLength={256}
                        value={form.name}
                        onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                        className="block w-full px-2 py-1.5 rounded border border-edge text-sm"
                      />
                    </div>
                    {!editingId && (
                      <div>
                        <label htmlFor="tenant-slug" className="block text-sm mb-1">
                          {t('tenants.admin.slug')} *
                        </label>
                        <input
                          id="tenant-slug"
                          type="text"
                          required
                          maxLength={128}
                          pattern="^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$"
                          value={form.slug}
                          onChange={(e) => setForm((f) => ({ ...f, slug: e.target.value.toLowerCase() }))}
                          className="block w-full px-2 py-1.5 rounded border border-edge text-sm"
                        />
                        <small className="text-muted text-xs">{t('tenants.admin.slugHelp')}</small>
                      </div>
                    )}
                    {editingId && (
                      <div>
                        <label className="block text-sm mb-1">{t('tenants.admin.slug')}</label>
                        <input
                          type="text"
                          disabled
                          value={form.slug}
                          className="block w-full px-2 py-1.5 rounded border border-edge text-sm opacity-50 cursor-not-allowed"
                        />
                        <small className="text-muted text-xs">{t('tenants.admin.slugImmutable')}</small>
                      </div>
                    )}
                  </div>

                  {!editingId && (
                    <div className="grid grid-cols-2 gap-3">
                      <div>
                        <label htmlFor="tenant-type" className="block text-sm mb-1">
                          {t('tenants.admin.tenantType')} *
                        </label>
                        <select
                          id="tenant-type"
                          required
                          value={form.tenantType}
                          onChange={(e) => setForm((f) => ({ ...f, tenantType: e.target.value }))}
                          className="block w-full px-2 py-1.5 rounded border border-edge text-sm"
                        >
                          {TENANT_TYPE_OPTIONS.map((t) => (
                            <option key={t} value={t}>{t}</option>
                          ))}
                        </select>
                      </div>
                      {(form.tenantType === 'Subsidiary' || form.tenantType === 'Department') && (
                        <div>
                          <label htmlFor="tenant-parent" className="block text-sm mb-1">
                            {t('tenants.admin.parentTenantId')}
                          </label>
                          <input
                            id="tenant-parent"
                            type="text"
                            placeholder="UUID do tenant pai"
                            value={form.parentTenantId}
                            onChange={(e) => setForm((f) => ({ ...f, parentTenantId: e.target.value }))}
                            className="block w-full px-2 py-1.5 rounded border border-edge text-sm"
                          />
                        </div>
                      )}
                    </div>
                  )}

                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label htmlFor="tenant-legal" className="block text-sm mb-1">
                        {t('tenants.admin.legalName')}
                      </label>
                      <input
                        id="tenant-legal"
                        type="text"
                        maxLength={512}
                        value={form.legalName}
                        onChange={(e) => setForm((f) => ({ ...f, legalName: e.target.value }))}
                        className="block w-full px-2 py-1.5 rounded border border-edge text-sm"
                      />
                    </div>
                    <div>
                      <label htmlFor="tenant-taxid" className="block text-sm mb-1">
                        {t('tenants.admin.taxId')}
                      </label>
                      <input
                        id="tenant-taxid"
                        type="text"
                        maxLength={50}
                        value={form.taxId}
                        onChange={(e) => setForm((f) => ({ ...f, taxId: e.target.value }))}
                        className="block w-full px-2 py-1.5 rounded border border-edge text-sm"
                      />
                    </div>
                  </div>

                  <div className="flex gap-2 justify-end">
                    <Button
                      type="button"
                      onClick={() => {
                        setShowForm(false);
                        setEditingId(null);
                        setForm(DEFAULT_FORM);
                      }}
                    >
                      {t('common.cancel')}
                    </Button>
                    <Button
                      type="submit"
                      disabled={createMutation.isPending || updateMutation.isPending}
                    >
                      {editingId ? t('common.save') : t('tenants.admin.createTenant')}
                    </Button>
                  </div>
                </div>
              </form>
            </CardBody>
          </Card>
        )}

        {/* Tenant List */}
        {tenants.length === 0 ? (
          <EmptyState
            title={t('tenants.admin.noTenantsFound')}
            action={
              <Button
                type="button"
                onClick={() => {
                  setForm(DEFAULT_FORM);
                  setEditingId(null);
                  setShowForm(true);
                }}
              >
                <Plus size={16} /> {t('tenants.admin.createTenant')}
              </Button>
            }
          />
        ) : (
          <div className="flex flex-col gap-2">
            {tenants.map((tenant) => (
              <Card key={tenant.id}>
                <CardBody>
                  <div className="flex items-center justify-between flex-wrap gap-2">
                    <div className="flex items-center gap-2.5 flex-wrap">
                      <Building2 size={16} className="text-faded shrink-0" />
                      <div>
                        <div className="flex items-center gap-1.5 flex-wrap">
                          <strong className="text-heading">{tenant.name}</strong>
                          <code className="text-[11px] bg-elevated px-1 rounded-sm text-muted">
                            {tenant.slug}
                          </code>
                        </div>
                        {tenant.legalName && (
                          <div className="text-xs text-muted">{tenant.legalName}</div>
                        )}
                        {tenant.taxId && (
                          <div className="text-xs text-faded">{t('tenants.admin.taxId')}: {tenant.taxId}</div>
                        )}
                      </div>
                      <Badge variant="default">{tenant.tenantType}</Badge>
                      <Badge variant={tenant.isActive ? 'success' : 'default'}>
                        {tenant.isActive ? t('common.active') : t('common.inactive')}
                      </Badge>
                    </div>
                    <div className="flex gap-2 shrink-0">
                      <Button
                        type="button"
                        onClick={() => handleEdit(tenant)}
                        title={t('common.edit')}
                      >
                        <Pencil size={14} />
                      </Button>
                      {tenant.isActive ? (
                        <Button
                          type="button"
                          onClick={() => handleDeactivate(tenant)}
                          disabled={deactivateMutation.isPending}
                          title={t('tenants.admin.deactivate')}
                        >
                          <ShieldOff size={14} /> {t('tenants.admin.deactivate')}
                        </Button>
                      ) : (
                        <Button
                          type="button"
                          onClick={() => handleActivate(tenant)}
                          disabled={activateMutation.isPending}
                          title={t('tenants.admin.activate')}
                        >
                          <ShieldCheck size={14} /> {t('tenants.admin.activate')}
                        </Button>
                      )}
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}

        {data && data.totalCount > tenants.length && (
          <div className="text-xs text-muted text-center mt-3">
            {t('tenants.admin.showingCount', { count: tenants.length, total: data.totalCount })}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
