import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface AppContentFrameProps {
  children: ReactNode;
  className?: string;
}

export function AppContentFrame({ children, className }: AppContentFrameProps) {
  return (
    <main
      className={cn('flex-1 overflow-y-auto', className)}
      id="main-content"
      role="main"
    >
      {children}
    </main>
  );
}
