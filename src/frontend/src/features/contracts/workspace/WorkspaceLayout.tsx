import type React from 'react';
import { cn } from '../../../lib/cn';

interface WorkspaceLayoutProps {
  header: React.ReactNode;
  identityCard: React.ReactNode;
  rail?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}

/** Shell estrutural do workspace de contrato (padrão v5): PageHeader + 3 colunas. */
export function WorkspaceLayout({ header, identityCard, rail, children, className }: WorkspaceLayoutProps) {
  return (
    <div className={cn('flex flex-col h-full', className)}>
      <div className="px-6 pt-6">{header}</div>
      <div className="flex-1 min-h-0 overflow-y-auto px-6 pb-6">
        <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)_240px] gap-6 items-start">
          <div className="lg:sticky lg:top-4">{identityCard}</div>
          <div className="min-w-0">{children}</div>
          {rail && <aside className="lg:sticky lg:top-4">{rail}</aside>}
        </div>
      </div>
    </div>
  );
}
