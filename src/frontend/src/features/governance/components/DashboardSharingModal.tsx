/**
 * DashboardSharingModal — modal para definir política de partilha granular de um dashboard.
 * V3.1 — Dashboard Intelligence Foundation.
 * Substitui o toggle booleano por seleção de âmbito e permissão.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { X, Share2, Globe, Users, Lock, Link, AlertTriangle, Check } from 'lucide-react';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

/** Mirrors DashboardSharingScope enum (backend) */
enum SharingScope {
  Private = 0,
  Team = 1,
  Tenant = 2,
  PublicLink = 3,
}

/** Mirrors DashboardSharingPermission enum (backend) */
enum SharingPermission {
  Read = 0,
  Edit = 1,
}

interface ShareDashboardPayload {
  dashboardId: string;
  tenantId: string;
  userId: string;
  scope: SharingScope;
  permission: SharingPermission;
  signedLinkExpiresAt?: string | null;
}

interface ShareDashboardResponse {
  dashboardId: string;
  scope: SharingScope;
  permission: SharingPermission;
  isVisible: boolean;
  signedLinkExpiresAt?: string | null;
}

// ── Hook ───────────────────────────────────────────────────────────────────

function useShareDashboard(dashboardId: string, tenantId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: Omit<ShareDashboardPayload, 'dashboardId' | 'tenantId' | 'userId'>) =>
      client
        .post<ShareDashboardResponse>(`/governance/dashboards/${dashboardId}/share`, {
          dashboardId,
          tenantId,
          userId: 'current-user',
          ...payload,
        })
        .then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['dashboard-render-data', dashboardId] });
      qc.invalidateQueries({ queryKey: ['governance-dashboards'] });
    },
  });
}

// ── Scope option definitions ───────────────────────────────────────────────

interface ScopeOption {
  scope: SharingScope;
  icon: React.ComponentType<{ size?: number; className?: string }>;
  labelKey: string;
  labelFallback: string;
  descriptionKey: string;
  descriptionFallback: string;
  warningKey?: string;
  warningFallback?: string;
}

const SCOPE_OPTIONS: ScopeOption[] = [
  {
    scope: SharingScope.Private,
    icon: Lock,
    labelKey: 'dashboardSharing.scopePrivate',
    labelFallback: 'Private',
    descriptionKey: 'dashboardSharing.scopePrivateDesc',
    descriptionFallback: 'Only you can see this dashboard',
  },
  {
    scope: SharingScope.Team,
    icon: Users,
    labelKey: 'dashboardSharing.scopeTeam',
    labelFallback: 'Team',
    descriptionKey: 'dashboardSharing.scopeTeamDesc',
    descriptionFallback: 'Visible to your team members',
  },
  {
    scope: SharingScope.Tenant,
    icon: Globe,
    labelKey: 'dashboardSharing.scopeTenant',
    labelFallback: 'Everyone in tenant',
    descriptionKey: 'dashboardSharing.scopeTenantDesc',
    descriptionFallback: 'Visible to all users in your organisation',
  },
  {
    scope: SharingScope.PublicLink,
    icon: Link,
    labelKey: 'dashboardSharing.scopePublicLink',
    labelFallback: 'Public link',
    descriptionKey: 'dashboardSharing.scopePublicLinkDesc',
    descriptionFallback: 'Anyone with the link can view',
    warningKey: 'dashboardSharing.warningPublicLink',
    warningFallback: 'Public links allow access without authentication. Use carefully.',
  },
];

// ── Component ──────────────────────────────────────────────────────────────

interface DashboardSharingModalProps {
  dashboardId: string;
  tenantId: string;
  currentScope: SharingScope;
  currentPermission: SharingPermission;
  isOpen: boolean;
  onClose: () => void;
}

export function DashboardSharingModal({
  dashboardId,
  tenantId,
  currentScope,
  currentPermission,
  isOpen,
  onClose,
}: DashboardSharingModalProps) {
  const { t } = useTranslation();
  const [scope, setScope] = useState<SharingScope>(currentScope);
  const [permission, setPermission] = useState<SharingPermission>(currentPermission);
  const [success, setSuccess] = useState(false);

  const shareMutation = useShareDashboard(dashboardId, tenantId);

  const handleSave = async () => {
    await shareMutation.mutateAsync({ scope, permission });
    setSuccess(true);
    setTimeout(() => {
      setSuccess(false);
      onClose();
    }, 1500);
  };

  if (!isOpen) return null;

  const selectedOption = SCOPE_OPTIONS.find((o) => o.scope === scope);

  return (
    <>
      {/* Overlay */}
      <div
        className="fixed inset-0 bg-black/50 z-50"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Modal */}
      <div
        className="fixed inset-0 z-50 flex items-center justify-center p-4"
        role="dialog"
        aria-modal="true"
        aria-label={t('dashboardSharing.title', 'Share Dashboard')}
      >
        <div className="bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-full max-w-md">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-2">
              <Share2 size={18} className="text-accent" />
              <h2 className="text-base font-semibold text-gray-900 dark:text-white">
                {t('dashboardSharing.title', 'Share Dashboard')}
              </h2>
            </div>
            <Button variant="ghost" size="sm" onClick={onClose}>
              <X size={16} />
            </Button>
          </div>

          {/* Body */}
          <div className="px-6 py-5 space-y-5">
            {/* Scope selector */}
            <div>
              <p className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
                {t('dashboardSharing.scopeLabel', 'Visibility')}
              </p>
              <div className="space-y-2">
                {SCOPE_OPTIONS.map((option) => {
                  const Icon = option.icon;
                  const isSelected = scope === option.scope;
                  return (
                    <button
                      key={option.scope}
                      onClick={() => setScope(option.scope)}
                      className={`w-full flex items-start gap-3 px-4 py-3 rounded-lg border text-left transition-colors ${
                        isSelected
                          ? 'border-accent bg-accent/5 dark:bg-accent/10'
                          : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
                      }`}
                    >
                      <Icon
                        size={16}
                        className={`mt-0.5 flex-shrink-0 ${isSelected ? 'text-accent' : 'text-gray-400 dark:text-gray-500'}`}
                      />
                      <div className="min-w-0">
                        <p className={`text-sm font-medium ${isSelected ? 'text-accent' : 'text-gray-700 dark:text-gray-200'}`}>
                          {t(option.labelKey, option.labelFallback)}
                        </p>
                        <p className="text-xs text-gray-400 dark:text-gray-500">
                          {t(option.descriptionKey, option.descriptionFallback)}
                        </p>
                      </div>
                      {isSelected && <Check size={14} className="ml-auto flex-shrink-0 text-accent mt-0.5" />}
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Warning for PublicLink */}
            {scope === SharingScope.PublicLink && selectedOption?.warningKey && (
              <div className="flex items-start gap-2 px-3 py-2.5 rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700">
                <AlertTriangle size={14} className="text-amber-500 flex-shrink-0 mt-0.5" />
                <p className="text-xs text-amber-700 dark:text-amber-300">
                  {t(selectedOption.warningKey, selectedOption.warningFallback ?? '')}
                </p>
              </div>
            )}

            {/* Permission (only when not Private) */}
            {scope !== SharingScope.Private && (
              <div>
                <p className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
                  {t('dashboardSharing.permissionLabel', 'Permission')}
                </p>
                <div className="flex gap-2">
                  {[
                    { perm: SharingPermission.Read, key: 'dashboardSharing.permissionRead', fallback: 'View only' },
                    { perm: SharingPermission.Edit, key: 'dashboardSharing.permissionEdit', fallback: 'Can edit' },
                  ].map(({ perm, key, fallback }) => (
                    <button
                      key={perm}
                      onClick={() => setPermission(perm)}
                      disabled={scope === SharingScope.PublicLink && perm === SharingPermission.Edit}
                      className={`flex-1 px-3 py-2 rounded-lg border text-sm font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed ${
                        permission === perm
                          ? 'border-accent bg-accent/5 text-accent'
                          : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:border-gray-300'
                      }`}
                    >
                      {t(key, fallback)}
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 dark:border-gray-700">
            <Button variant="ghost" size="sm" onClick={onClose}>
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={handleSave}
              loading={shareMutation.isPending}
              disabled={success}
            >
              {success ? (
                <><Check size={14} /> {t('dashboardSharing.success', 'Saved')}</>
              ) : (
                t('dashboardSharing.save', 'Save sharing settings')
              )}
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
