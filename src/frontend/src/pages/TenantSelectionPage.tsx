import { useState } from 'react';
import { useNavigate, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Building2, ChevronRight } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { Button } from '../components/Button';
import { resolveApiError } from '../utils/apiErrors';
import type { TenantInfo } from '../types';

/**
 * Página de seleção de tenant — exibida após login quando o usuário
 * possui múltiplos tenants ativos.
 *
 * Apresenta lista amigável com nome, slug e papel do usuário em cada tenant.
 * Após seleção, emite novo JWT com contexto do tenant escolhido e redireciona ao dashboard.
 */
export function TenantSelectionPage() {
  const { t } = useTranslation();
  const { requiresTenantSelection, availableTenants, selectTenant, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  if (!requiresTenantSelection) {
    return <Navigate to={isAuthenticated ? '/' : '/login'} replace />;
  }

  const handleSelect = async (tenant: TenantInfo) => {
    setError(null);
    setLoading(tenant.id);
    try {
      await selectTenant(tenant.id);
      navigate('/');
    } catch (err) {
      setError(resolveApiError(err));
    } finally {
      setLoading(null);
    }
  };

  return (
    <div className="min-h-screen bg-canvas flex items-center justify-center p-4">
      <div className="w-full max-w-lg animate-fade-in">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-14 h-14 rounded-xl bg-accent/15 mb-4">
            <Building2 size={28} className="text-accent" />
          </div>
          <h1 className="text-2xl font-bold text-heading">{t('tenants.selectTenant')}</h1>
          <p className="text-muted mt-1">{t('tenants.selectTenantDescription')}</p>
        </div>

        {error && (
          <div className="mb-4 rounded-md bg-critical/10 border border-critical/30 px-4 py-3 text-sm text-critical">
            {error}
          </div>
        )}

        {/* Tenant List */}
        <div className="space-y-3">
          {availableTenants.map((tenant) => (
            <button
              key={tenant.id}
              onClick={() => handleSelect(tenant)}
              disabled={!tenant.isActive || loading !== null}
              className={`w-full bg-card rounded-xl border px-6 py-4 flex items-center gap-4 text-left transition-all ${
                tenant.isActive
                  ? 'border-edge hover:border-accent/40 hover:shadow-glow cursor-pointer'
                  : 'border-edge opacity-50 cursor-not-allowed'
              }`}
            >
              <div className="w-10 h-10 rounded-lg bg-accent/15 flex items-center justify-center text-accent font-bold text-sm shrink-0">
                {tenant.name[0]?.toUpperCase() ?? 'T'}
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-heading truncate">{tenant.name}</p>
                <p className="text-sm text-muted truncate">
                  {tenant.slug}
                  {!tenant.isActive && (
                    <span className="ml-2 text-xs text-critical font-medium">{t('tenants.inactive')}</span>
                  )}
                </p>
              </div>
              <div className="text-right shrink-0">
                <span className="text-xs text-faded">{t('tenants.role')}</span>
                <p className="text-sm font-medium text-body">{tenant.roleName}</p>
              </div>
              {loading === tenant.id ? (
                <div className="w-5 h-5 border-2 border-accent border-t-transparent rounded-full animate-spin shrink-0" />
              ) : (
                <ChevronRight size={18} className="text-faded shrink-0" />
              )}
            </button>
          ))}
        </div>

        {availableTenants.length === 0 && (
          <div className="text-center py-12">
            <p className="text-muted">{t('tenants.noTenants')}</p>
            <Button onClick={() => navigate('/login')} variant="secondary" className="mt-4">
              {t('auth.signInButton')}
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
