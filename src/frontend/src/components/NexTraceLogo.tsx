import { useTranslation } from 'react-i18next';
import { cn } from '../lib/cn';

interface NexTraceLogoProps {
  /** Tamanho total do ícone (largura e altura em px). Default: 40 */
  size?: number;
  /** Mostrar apenas o ícone sem o wordmark */
  iconOnly?: boolean;
  /** Classe extra aplicada ao wrapper */
  className?: string;
  /** Variante do wordmark — 'full' inclui tagline, 'compact' apenas o nome */
  variant?: 'full' | 'compact' | 'icon';
}

/**
 * NexTraceLogo — marca oficial do produto.
 *
 * Representa o globo com malha de rede e a letra N com gradiente
 * azul → cyan → mint, alinhado com a identidade visual oficial.
 *
 * Variantes:
 * - icon: apenas o ícone do globo
 * - compact: ícone + wordmark "NexTraceOne"
 * - full: ícone + wordmark + tagline "Operational Confidence"
 */
export function NexTraceLogo({ size = 40, iconOnly = false, className, variant = 'compact' }: NexTraceLogoProps) {
  const { t } = useTranslation();
  const showWordmark = variant !== 'icon' && !iconOnly;
  const showTagline = variant === 'full';

  return (
    <div className={cn('flex items-center gap-3', className)}>
      <NexTraceIcon size={size} />
      {showWordmark && (
        <div className="flex flex-col leading-none">
          <NexTraceWordmark size={size} />
          {showTagline && (
            <span
              className="text-accent font-medium tracking-wide mt-0.5"
              style={{ fontSize: Math.max(10, size * 0.26) }}
            >
              Operational Confidence
            </span>
          )}
        </div>
      )}
    </div>
  );
}

/**
 * Ícone vetorial do globo NexTraceOne com gradiente e malha de rede.
 * Glyph autossuficiente, reutilizável em qualquer contexto.
 */
export function NexTraceIcon({ size = 40, className }: { size?: number; className?: string }) {
  const { t } = useTranslation();
  const id = `nto-logo-${size}`;

  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 40 40"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-label={t('brand.name', 'NexTraceOne')}
      className={className}
    >
      <defs>
        <linearGradient id={`${id}-grad`} x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#1B7FE8" />
          <stop offset="50%" stopColor="#12C4E8" />
          <stop offset="100%" stopColor="#18E8B8" />
        </linearGradient>
        <linearGradient id={`${id}-grad2`} x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#12C4E8" />
          <stop offset="100%" stopColor="#18E8B8" />
        </linearGradient>
        <clipPath id={`${id}-clip`}>
          <circle cx="20" cy="20" r="18" />
        </clipPath>
      </defs>

      {/* Globo — círculo principal */}
      <circle cx="20" cy="20" r="18" stroke={`url(#${id}-grad)`} strokeWidth="1.5" fill="none" opacity="0.9" />

      {/* Linhas de latitude */}
      <ellipse cx="20" cy="20" rx="18" ry="8" stroke={`url(#${id}-grad)`} strokeWidth="0.8" fill="none" opacity="0.35" />

      {/* Linha vertical central */}
      <line x1="20" y1="2" x2="20" y2="38" stroke={`url(#${id}-grad)`} strokeWidth="0.8" opacity="0.35" />

      {/* Nós de rede (círculos) */}
      <circle cx="20" cy="4"  r="2"   fill={`url(#${id}-grad2)`} opacity="0.9" />
      <circle cx="6"  cy="13" r="2"   fill={`url(#${id}-grad2)`} opacity="0.75" />
      <circle cx="35" cy="14" r="1.5" fill={`url(#${id}-grad2)`} opacity="0.75" />
      <circle cx="32" cy="28" r="2"   fill={`url(#${id}-grad2)`} opacity="0.9" />

      {/* Arco de conexão — traço de rede superior */}
      <path
        d="M 20 4 Q 30 8 35 14"
        stroke={`url(#${id}-grad)`}
        strokeWidth="1.2"
        fill="none"
        strokeLinecap="round"
        opacity="0.85"
      />
      <path
        d="M 6 13 Q 12 9 20 4"
        stroke={`url(#${id}-grad)`}
        strokeWidth="1.2"
        fill="none"
        strokeLinecap="round"
        opacity="0.6"
      />
      <path
        d="M 35 14 Q 36 22 32 28"
        stroke={`url(#${id}-grad2)`}
        strokeWidth="1.2"
        fill="none"
        strokeLinecap="round"
        opacity="0.75"
      />

      {/* Letra N — glifo central com gradiente */}
      <text
        x="20"
        y="26"
        textAnchor="middle"
        fontSize="16"
        fontWeight="700"
        fontFamily="Inter, system-ui, sans-serif"
        fill={`url(#${id}-grad)`}
        letterSpacing="-0.5"
      >
        N
      </text>
    </svg>
  );
}

/**
 * Wordmark tipográfico "NexTraceOne" com coloração de marca.
 * "Nex" em azul-cyan, "Trace" em branco bold, "One" em cyan-mint.
 */
export function NexTraceWordmark({ size = 40 }: { size?: number }) {
  const { t } = useTranslation();
  const fontSize = Math.max(12, size * 0.38);

  return (
    <span
      className="font-bold tracking-tight leading-none"
      style={{ fontSize }}
      aria-label={t('brand.name', 'NexTraceOne')}
    >
      <span className="text-blue">Nex</span>
      <span className="text-heading">Trace</span>
      <span className="text-cyan">One</span>
    </span>
  );
}
