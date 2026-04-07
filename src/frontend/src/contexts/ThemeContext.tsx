/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';

/**
 * Preference as stored — 'auto' follows the OS color-scheme.
 * resolvedTheme always returns a concrete 'light' | 'dark'.
 */
type ThemePreference = 'light' | 'dark' | 'auto';
type ResolvedTheme = 'light' | 'dark';

interface ThemeContextValue {
  /** Current preference (may be 'auto'). */
  theme: ThemePreference;
  /** Concrete theme applied to the DOM. */
  resolvedTheme: ResolvedTheme;
  toggleTheme: () => void;
  setTheme: (mode: ThemePreference) => void;
}

const STORAGE_KEY = 'nto-theme';

const ThemeContext = createContext<ThemeContextValue | null>(null);

/**
 * Returns system color-scheme preference.
 * Falls back to 'dark' when window or matchMedia is unavailable (e.g. SSR, test environments).
 * Dark-first enterprise default per DESIGN-SYSTEM.md.
 */
function getSystemPreference(): ResolvedTheme {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return 'dark';
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function getStoredTheme(): ThemePreference | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'auto') return stored;
  } catch {
    // localStorage unavailable
  }
  return null;
}

function resolve(pref: ThemePreference): ResolvedTheme {
  return pref === 'auto' ? getSystemPreference() : pref;
}

/**
 * Applies theme to the DOM.
 * Sets `data-theme` attribute (controls CSS variable selection) and
 * `colorScheme` style (informs browser for native UI elements like scrollbars and form controls).
 */
function applyThemeToDOM(theme: ResolvedTheme): void {
  const root = document.documentElement;
  root.setAttribute('data-theme', theme);
  root.style.colorScheme = theme;
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreference] = useState<ThemePreference>(() => {
    return getStoredTheme() ?? 'auto';
  });

  // resolvedTheme is derived synchronously from preference — no effect needed
  const resolvedTheme: ResolvedTheme = resolve(preference);

  // Apply to DOM whenever the resolved theme changes
  useEffect(() => {
    applyThemeToDOM(resolvedTheme);
  }, [resolvedTheme]);

  // Force re-render when system preference changes while 'auto' is active
  const [, triggerRerender] = useState(0);

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return;
    const mq = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = () => {
      // Only trigger re-render when in auto mode (so resolve() picks up new system pref)
      if (preference === 'auto' || !getStoredTheme()) {
        triggerRerender(c => c + 1);
      }
    };
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, [preference]);

  const setTheme = useCallback((mode: ThemePreference) => {
    setPreference(mode);
    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // localStorage unavailable
    }
  }, []);

  // Cycle: dark → light → auto → dark
  const toggleTheme = useCallback(() => {
    const next: ThemePreference = preference === 'dark' ? 'light' : preference === 'light' ? 'auto' : 'dark';
    setTheme(next);
  }, [preference, setTheme]);

  return (
    <ThemeContext.Provider value={{ theme: preference, resolvedTheme, toggleTheme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider');
  return ctx;
}
