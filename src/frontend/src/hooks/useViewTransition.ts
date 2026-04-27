import { useCallback } from 'react';

/**
 * useViewTransition — wraps navigation callbacks with the View Transitions API
 * when supported by the browser (Wave V3.5 — Frontend Platform Uplift).
 *
 * Falls back gracefully to a plain synchronous call when the API is unavailable.
 *
 * Usage:
 *   const withTransition = useViewTransition();
 *   withTransition(() => navigate('/some/route'));
 */
export function useViewTransition() {
  return useCallback(<T>(callback: () => T): T | undefined => {
    if (typeof document !== 'undefined' && 'startViewTransition' in document) {
      let result: T | undefined;
      (document as unknown as { startViewTransition(cb: () => void): void })
        .startViewTransition(() => {
          result = callback();
        });
      return result;
    }
    return callback();
  }, []);
}
