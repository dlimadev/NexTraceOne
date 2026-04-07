import { useState, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { cn } from '@/lib/cn';
import { identityApi } from '@/features/identity-access/api/identity';
import type { RoleInfo } from '@/types';

interface RolePickerProps {
  /** Configuration key for i18n label resolution */
  configKey: string;
  /** Currently selected role names */
  value: string[];
  /** Callback when selection changes */
  onChange: (roles: string[]) => void;
  /** Whether the picker is read-only */
  readOnly?: boolean;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Multi-select component for configuration parameters that reference roles.
 * Fetches available roles from the API and allows selection with visual feedback.
 *
 * Parameters referencing roles must use RolePicker, not free-text JSON.
 * Shows both system roles and custom tenant roles, validates that selected roles exist.
 *
 * @example
 * ```tsx
 * <RolePicker
 *   configKey="catalog.service.creation.approval_roles"
 *   value={["TechLead", "Architect"]}
 *   onChange={(roles) => handleRolesChange(roles)}
 * />
 * ```
 */
export function RolePicker({ configKey, value, onChange, readOnly = false, className }: RolePickerProps) {
  const { t } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const { data: roles = [], isLoading } = useQuery<RoleInfo[]>({
    queryKey: ['identity', 'roles'],
    queryFn: () => identityApi.listRoles(),
    staleTime: 10 * 60 * 1000,
  });

  const label = t(`config.${configKey}.label`, { defaultValue: configKey });
  const description = t(`config.${configKey}.description`, { defaultValue: '' });

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const toggleRole = (roleName: string) => {
    if (readOnly) return;
    const newValue = value.includes(roleName)
      ? value.filter((r) => r !== roleName)
      : [...value, roleName];
    onChange(newValue);
  };

  return (
    <div className={cn('py-3', className)} ref={dropdownRef}>
      <div className="mb-2">
        <p className="text-sm font-medium text-heading">{label}</p>
        {description && (
          <p className="mt-0.5 text-xs text-body">{description}</p>
        )}
      </div>

      <div className="relative">
        <button
          type="button"
          onClick={() => !readOnly && setIsOpen(!isOpen)}
          disabled={readOnly}
          aria-label={label}
          aria-expanded={isOpen}
          className={cn(
            'flex w-full flex-wrap items-center gap-1.5 rounded-md border px-3 py-1.5',
            'min-h-[38px] border-elevated bg-canvas text-left',
            'focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent',
            readOnly && 'cursor-not-allowed opacity-50',
          )}
        >
          {value.length === 0 ? (
            <span className="text-sm text-muted">
              {t('config.rolePicker.placeholder', { defaultValue: 'Select roles...' })}
            </span>
          ) : (
            value.map((roleName) => (
              <span
                key={roleName}
                className="inline-flex items-center gap-1 rounded-md bg-cyan/10 px-2 py-0.5 text-xs font-medium text-cyan"
              >
                {roleName}
                {!readOnly && (
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleRole(roleName);
                    }}
                    className="hover:text-cyan/70"
                    aria-label={t('config.rolePicker.remove', { role: roleName, defaultValue: `Remove ${roleName}` })}
                  >
                    ×
                  </button>
                )}
              </span>
            ))
          )}
        </button>

        {isOpen && (
          <div className="absolute z-50 mt-1 max-h-60 w-full overflow-auto rounded-md border border-elevated bg-canvas shadow-lg">
            {isLoading ? (
              <div className="px-3 py-2 text-sm text-muted">
                {t('common.loading', { defaultValue: 'Loading...' })}
              </div>
            ) : roles.length === 0 ? (
              <div className="px-3 py-2 text-sm text-muted">
                {t('config.rolePicker.noRoles', { defaultValue: 'No roles available' })}
              </div>
            ) : (
              roles.map((role) => {
                const isSelected = value.includes(role.name);
                return (
                  <button
                    key={role.id}
                    type="button"
                    onClick={() => toggleRole(role.name)}
                    className={cn(
                      'flex w-full items-center gap-3 px-3 py-2 text-left text-sm',
                      'hover:bg-elevated/50',
                      isSelected && 'bg-cyan/5',
                    )}
                  >
                    <span
                      className={cn(
                        'flex h-4 w-4 shrink-0 items-center justify-center rounded border',
                        isSelected
                          ? 'border-cyan bg-cyan text-white'
                          : 'border-elevated',
                      )}
                    >
                      {isSelected && (
                        <svg className="h-3 w-3" viewBox="0 0 12 12" fill="none">
                          <path d="M10 3L4.5 8.5L2 6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                        </svg>
                      )}
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-heading">{role.name}</span>
                        {role.isSystem && (
                          <span className="inline-flex items-center rounded bg-elevated px-1.5 py-0.5 text-[10px] font-medium text-muted">
                            {t('config.rolePicker.system', { defaultValue: 'System' })}
                          </span>
                        )}
                      </div>
                      {role.description && (
                        <p className="truncate text-xs text-body">{role.description}</p>
                      )}
                    </div>
                  </button>
                );
              })
            )}
          </div>
        )}
      </div>
    </div>
  );
}
