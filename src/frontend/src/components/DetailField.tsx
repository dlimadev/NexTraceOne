import type { ReactNode } from 'react';
import { Copy, ExternalLink } from 'lucide-react';
import { cn } from '../lib/cn';
import { Tooltip } from './Tooltip';

interface DetailFieldProps {
  /** Label do campo. */
  label: string;
  /** Valor do campo. */
  value?: ReactNode;
  /** Layout: inline (label + value lado a lado) ou stacked (label em cima). */
  layout?: 'inline' | 'stacked';
  /** Permite copiar o valor para o clipboard. */
  copyable?: boolean;
  /** Texto a copiar (se diferente do value renderizado). */
  copyText?: string;
  /** Tooltip sobre o valor. */
  tooltip?: string;
  /** URL para tornar o valor num link. */
  href?: string;
  /** Se o link é externo (abre em nova tab). */
  external?: boolean;
  /** Mono font para valores técnicos. */
  mono?: boolean;
  className?: string;
}

/**
 * Campo de detalhe label/valor reutilizável.
 *
 * Usado em páginas de detalhe (ServiceDetailPage, ContractDetailPage, etc.)
 * para exibir informações estruturadas com consistência visual.
 *
 * Suporta: copyable, tooltip, link, layout inline/stacked, mono font.
 */
export function DetailField({
  label,
  value,
  layout = 'stacked',
  copyable = false,
  copyText,
  tooltip,
  href,
  external = false,
  mono = false,
  className,
}: DetailFieldProps) {
  const handleCopy = () => {
    const text = copyText ?? (typeof value === 'string' ? value : '');
    if (text) navigator.clipboard.writeText(text);
  };

  const valueContent = (
    <span className={cn('text-sm text-heading', mono && 'font-mono')}>
      {href ? (
        <a
          href={href}
          target={external ? '_blank' : undefined}
          rel={external ? 'noopener noreferrer' : undefined}
          className="text-accent hover:underline inline-flex items-center gap-1"
        >
          {value ?? '—'}
          {external && <ExternalLink size={12} aria-hidden="true" />}
        </a>
      ) : (
        value ?? <span className="text-muted">—</span>
      )}
    </span>
  );

  const wrappedValue = tooltip ? (
    <Tooltip content={tooltip}>{valueContent}</Tooltip>
  ) : (
    valueContent
  );

  if (layout === 'inline') {
    return (
      <div className={cn('flex items-center justify-between gap-4 py-2', className)}>
        <span className="text-xs font-medium text-muted shrink-0">{label}</span>
        <div className="flex items-center gap-1.5 min-w-0">
          <span className="truncate">{wrappedValue}</span>
          {copyable && (
            <button
              type="button"
              onClick={handleCopy}
              className="shrink-0 p-1 text-muted hover:text-body rounded transition-colors"
              aria-label={`Copy ${label}`}
            >
              <Copy size={12} />
            </button>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className={cn('space-y-1', className)}>
      <span className="text-xs font-medium text-muted block">{label}</span>
      <div className="flex items-center gap-1.5">
        {wrappedValue}
        {copyable && (
          <button
            type="button"
            onClick={handleCopy}
            className="shrink-0 p-1 text-muted hover:text-body rounded transition-colors"
            aria-label={`Copy ${label}`}
          >
            <Copy size={12} />
          </button>
        )}
      </div>
    </div>
  );
}
