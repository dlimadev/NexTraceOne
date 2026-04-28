import { useEffect, useRef, useSyncExternalStore } from 'react';
import { useLocation } from 'react-router-dom';

/**
 * NProgress-style loading bar shown at the very top during route transitions.
 * Uses the NexTraceOne brand gradient (blue→cyan→mint) and respects prefers-reduced-motion.
 *
 * Implementation uses useSyncExternalStore to avoid calling setState inside effects
 * (react-hooks/set-state-in-effect).
 */

// Module-level store to avoid setState inside effects
let barVisible = false;
const listeners = new Set<() => void>();
function subscribe(cb: () => void) { listeners.add(cb); return () => listeners.delete(cb); }
function getSnapshot() { return barVisible; }
function show() { barVisible = true; listeners.forEach(cb => cb()); }
function hide() { barVisible = false; listeners.forEach(cb => cb()); }

export function RouteProgressBar() {
  const { pathname } = useLocation();
  const prevPath = useRef(pathname);
  const timerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const visible = useSyncExternalStore(subscribe, getSnapshot, getSnapshot);

  useEffect(() => {
    if (pathname === prevPath.current) return;
    prevPath.current = pathname;
    show();
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(hide, 500);
    return () => { if (timerRef.current) clearTimeout(timerRef.current); };
  }, [pathname]);

  if (!visible) return null;

  return (
    <div
      className="fixed top-0 left-0 right-0 h-0.5 z-[9999] motion-reduce:hidden"
      role="progressbar"
      aria-label="Loading"
    >
      <div className="h-full bg-gradient-to-r from-blue via-cyan to-mint animate-progress-bar" />
    </div>
  );
}
