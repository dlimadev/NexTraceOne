/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useEffect, useMemo, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { configurationApi } from '@/features/configuration/api/configurationApi';
import type { EffectiveConfigurationDto } from '@/features/configuration/types';

/**
 * BrandingContext — contexto de personalização visual da marca da plataforma.
 *
 * Carrega os parâmetros de branding (logo, accent color, favicon, footer, welcome message)
 * a partir da API de configuração e aplica-os dinamicamente no DOM.
 *
 * Pilar: Platform Customization + Source of Truth
 */

interface BrandingValues {
  /** URL for the organization logo (light variant) */
  logoUrl: string;
  /** URL for the organization logo (dark variant) */
  logoDarkUrl: string;
  /** Primary accent color in hex format (e.g., "#3B82F6") */
  accentColor: string;
  /** URL for the custom favicon */
  faviconUrl: string;
  /** Custom welcome message displayed on the dashboard */
  welcomeMessage: string;
  /** Custom footer text displayed on all pages */
  footerText: string;
  /** Instance name */
  instanceName: string;
  /** Whether branding data has been loaded */
  isLoaded: boolean;
}

const DEFAULT_BRANDING: BrandingValues = {
  logoUrl: '',
  logoDarkUrl: '',
  accentColor: '',
  faviconUrl: '',
  welcomeMessage: '',
  footerText: '',
  instanceName: 'NexTraceOne',
  isLoaded: false,
};

const BrandingContext = createContext<BrandingValues>(DEFAULT_BRANDING);

const BRANDING_KEYS = [
  'branding.logo_url',
  'branding.logo_dark_url',
  'branding.accent_color',
  'branding.favicon_url',
  'branding.welcome_message',
  'branding.footer_text',
  'instance.name',
];

function extractValue(
  settings: EffectiveConfigurationDto[],
  key: string,
): string {
  const setting = settings.find((s) => s.key === key);
  return setting?.effectiveValue ?? '';
}

/**
 * Applies the accent color to CSS custom properties on the document root.
 * This enables dynamic theming based on the branding.accent_color parameter.
 */
function applyAccentColor(hex: string): void {
  if (!hex || !/^#[0-9A-Fa-f]{6}$/.test(hex)) return;

  const root = document.documentElement;
  root.style.setProperty('--nto-brand-accent', hex);

  // Parse hex to RGB for rgba() variants
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);

  root.style.setProperty('--nto-brand-accent-rgb', `${r}, ${g}, ${b}`);
  root.style.setProperty(
    '--nto-brand-accent-muted',
    `rgba(${r}, ${g}, ${b}, 0.10)`,
  );
  root.style.setProperty(
    '--nto-brand-accent-hover',
    adjustBrightness(hex, -15),
  );
}

/** Adjusts brightness of a hex color by a percentage (-100 to 100). */
function adjustBrightness(hex: string, percent: number): string {
  const r = Math.max(0, Math.min(255, parseInt(hex.slice(1, 3), 16) + Math.round(2.55 * percent)));
  const g = Math.max(0, Math.min(255, parseInt(hex.slice(3, 5), 16) + Math.round(2.55 * percent)));
  const b = Math.max(0, Math.min(255, parseInt(hex.slice(5, 7), 16) + Math.round(2.55 * percent)));
  return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
}

/**
 * Applies a custom favicon to the document head.
 * Falls back gracefully if no URL is configured.
 */
function applyFavicon(url: string): void {
  if (!url) return;
  const existing = document.querySelector<HTMLLinkElement>('link[rel="icon"]');
  if (existing) {
    existing.href = url;
  } else {
    const link = document.createElement('link');
    link.rel = 'icon';
    link.href = url;
    document.head.appendChild(link);
  }
}

export function BrandingProvider({ children }: { children: ReactNode }) {
  const { data: settings } = useQuery({
    queryKey: ['branding-settings'],
    queryFn: async () => {
      const settled = await Promise.allSettled(
        BRANDING_KEYS.map((key) =>
          configurationApi.getEffectiveSettings('System', null, key),
        ),
      );
      return settled.flatMap((r) =>
        r.status === 'fulfilled' ? r.value : [],
      );
    },
    staleTime: 10 * 60 * 1000,
    gcTime: 60 * 60 * 1000,
    retry: 1,
  });

  const branding = useMemo<BrandingValues>(() => {
    if (!settings || settings.length === 0) return DEFAULT_BRANDING;

    return {
      logoUrl: extractValue(settings, 'branding.logo_url'),
      logoDarkUrl: extractValue(settings, 'branding.logo_dark_url'),
      accentColor: extractValue(settings, 'branding.accent_color'),
      faviconUrl: extractValue(settings, 'branding.favicon_url'),
      welcomeMessage: extractValue(settings, 'branding.welcome_message'),
      footerText: extractValue(settings, 'branding.footer_text'),
      instanceName: extractValue(settings, 'instance.name') || 'NexTraceOne',
      isLoaded: true,
    };
  }, [settings]);

  // Apply dynamic accent color
  useEffect(() => {
    if (branding.accentColor) {
      applyAccentColor(branding.accentColor);
    }
  }, [branding.accentColor]);

  // Apply dynamic favicon
  useEffect(() => {
    if (branding.faviconUrl) {
      applyFavicon(branding.faviconUrl);
    }
  }, [branding.faviconUrl]);

  return (
    <BrandingContext.Provider value={branding}>
      {children}
    </BrandingContext.Provider>
  );
}

export function useBranding(): BrandingValues {
  return useContext(BrandingContext);
}
