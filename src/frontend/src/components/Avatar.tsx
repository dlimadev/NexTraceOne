import { useMemo, type ReactNode } from 'react';
import { cn } from '../lib/cn';

/* ─── Types ─────────────────────────────────────────────────────────────────── */

type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

interface AvatarProps {
  /** URL da imagem. */
  src?: string;
  /** Nome completo (para iniciais e alt text). */
  name?: string;
  /** Tamanho do avatar. */
  size?: AvatarSize;
  /** Status indicator. */
  status?: 'online' | 'offline' | 'busy' | 'away';
  /** Ícone fallback customizado. */
  fallbackIcon?: ReactNode;
  className?: string;
}

interface AvatarGroupProps {
  /** Avatares filhos. */
  children: ReactNode;
  /** Máximo de avatares visíveis. */
  max?: number;
  /** Total de itens (para mostrar "+N"). */
  total?: number;
  /** Tamanho dos avatares. */
  size?: AvatarSize;
  className?: string;
}

/* ─── Constants ─────────────────────────────────────────────────────────────── */

const sizeClasses: Record<AvatarSize, string> = {
  xs: 'w-6 h-6 text-[9px]',
  sm: 'w-8 h-8 text-[10px]',
  md: 'w-10 h-10 text-xs',
  lg: 'w-12 h-12 text-sm',
  xl: 'w-16 h-16 text-base',
};

const statusSizeClasses: Record<AvatarSize, string> = {
  xs: 'w-1.5 h-1.5 border',
  sm: 'w-2 h-2 border',
  md: 'w-2.5 h-2.5 border-2',
  lg: 'w-3 h-3 border-2',
  xl: 'w-3.5 h-3.5 border-2',
};

const statusColorClasses: Record<NonNullable<AvatarProps['status']>, string> = {
  online: 'bg-success',
  offline: 'bg-neutral',
  busy: 'bg-danger',
  away: 'bg-warning',
};

/**
 * Cores determinísticas por hash do nome.
 * Garante que o mesmo nome sempre gera a mesma cor.
 */
const avatarColors = [
  'bg-blue/20 text-blue',
  'bg-cyan/20 text-cyan',
  'bg-mint/20 text-mint',
  'bg-warning/20 text-warning',
  'bg-critical/20 text-critical',
  'bg-info/20 text-info',
  'bg-accent/20 text-accent',
];

function hashName(name: string): number {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = (hash << 5) - hash + name.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0][0]?.toUpperCase() ?? '?';
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

/* ─── Avatar ────────────────────────────────────────────────────────────────── */

/**
 * Avatar component com imagem, iniciais ou ícone fallback.
 *
 * Tamanhos: xs (24px), sm (32px), md (40px), lg (48px), xl (64px).
 * Cores determinísticas por nome. Status indicator opcional.
 */
export function Avatar({
  src,
  name,
  size = 'md',
  status,
  fallbackIcon,
  className,
}: AvatarProps) {
  const colorClass = useMemo(() => {
    if (!name) return 'bg-elevated text-muted';
    return avatarColors[hashName(name) % avatarColors.length];
  }, [name]);

  const initials = useMemo(() => name ? getInitials(name) : '?', [name]);

  return (
    <div className={cn('relative inline-flex shrink-0', className)}>
      <div
        className={cn(
          'inline-flex items-center justify-center rounded-full font-semibold',
          'border border-edge overflow-hidden',
          sizeClasses[size],
          !src && colorClass,
        )}
        title={name}
        aria-label={name}
      >
        {src ? (
          <img
            src={src}
            alt={name ?? 'Avatar'}
            className="w-full h-full object-cover"
            onError={(e) => {
              // Fallback to initials if image fails
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
        ) : fallbackIcon ? (
          <span aria-hidden="true">{fallbackIcon}</span>
        ) : (
          <span aria-hidden="true">{initials}</span>
        )}
      </div>

      {status && (
        <span
          className={cn(
            'absolute bottom-0 right-0 rounded-full border-card',
            statusSizeClasses[size],
            statusColorClasses[status],
          )}
          aria-label={status}
        />
      )}
    </div>
  );
}

/* ─── AvatarGroup ───────────────────────────────────────────────────────────── */

/**
 * Grupo de avatares empilhados com overflow "+N".
 */
export function AvatarGroup({
  children,
  max = 4,
  total,
  size = 'md',
  className,
}: AvatarGroupProps) {
  const childArray = Array.isArray(children) ? children : [children];
  const visible = childArray.slice(0, max);
  const overflow = total ? total - max : childArray.length - max;

  return (
    <div className={cn('flex items-center -space-x-2', className)}>
      {visible.map((child, i) => (
        <div key={i} className="relative ring-2 ring-card rounded-full">
          {child}
        </div>
      ))}
      {overflow > 0 && (
        <div
          className={cn(
            'inline-flex items-center justify-center rounded-full bg-elevated text-muted font-semibold',
            'border border-edge ring-2 ring-card',
            sizeClasses[size],
          )}
        >
          +{overflow}
        </div>
      )}
    </div>
  );
}
