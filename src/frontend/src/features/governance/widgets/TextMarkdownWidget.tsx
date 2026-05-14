/**
 * TextMarkdownWidget — exibe conteúdo estático de texto/notas com formatação básica.
 * O conteúdo é guardado no config.content do WidgetSlot (sem chamada API).
 * Suporta negrito (**), itálico (*), cabeçalhos (#, ##, ###), listas (- ), links.
 * Útil para notas operacionais, runbook links, disclaimers e separadores de secção.
 */
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { FileText } from 'lucide-react';
import type { WidgetProps } from './WidgetRegistry';

// ── Simple inline markdown parser (no external dependency) ────────────────

/** Escape HTML entities to prevent XSS in dynamic content */
function escapeHtml(raw: string): string {
  return raw
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/** Apply inline formatting: **bold**, *italic*, `code`, [label](url) */
function applyInlineFormatting(text: string): string {
  return escapeHtml(text)
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.+?)\*/g, '<em>$1</em>')
    .replace(/`(.+?)`/g, '<code class="text-xs bg-gray-100 dark:bg-gray-800 px-0.5 rounded font-mono">$1</code>')
    .replace(
      /\[([^\]]+)\]\((https?:\/\/[^)]+)\)/g,
      '<a href="$2" target="_blank" rel="noopener noreferrer" class="text-accent underline">$1</a>',
    );
}

/** Convert lightweight markdown string to an array of HTML line strings */
function parseMarkdown(raw: string): string[] {
  const lines = raw.split('\n');
  const result: string[] = [];
  let inList = false;

  for (const line of lines) {
    const trimmed = line.trim();

    if (trimmed === '' || trimmed === '---' || trimmed === '***') {
      if (inList) { result.push('</ul>'); inList = false; }
      if (trimmed === '---' || trimmed === '***') {
        result.push('<hr class="border-gray-200 dark:border-gray-700 my-1" />');
      } else {
        result.push('<div class="h-1"></div>');
      }
      continue;
    }

    if (trimmed.startsWith('### ')) {
      if (inList) { result.push('</ul>'); inList = false; }
      result.push(`<h3 class="text-xs font-bold text-gray-800 dark:text-gray-200 uppercase tracking-wide leading-tight">${applyInlineFormatting(trimmed.slice(4))}</h3>`);
    } else if (trimmed.startsWith('## ')) {
      if (inList) { result.push('</ul>'); inList = false; }
      result.push(`<h2 class="text-sm font-bold text-gray-900 dark:text-white leading-tight">${applyInlineFormatting(trimmed.slice(3))}</h2>`);
    } else if (trimmed.startsWith('# ')) {
      if (inList) { result.push('</ul>'); inList = false; }
      result.push(`<h1 class="text-base font-bold text-gray-900 dark:text-white leading-tight">${applyInlineFormatting(trimmed.slice(2))}</h1>`);
    } else if (trimmed.startsWith('- ') || trimmed.startsWith('* ')) {
      if (!inList) { result.push('<ul class="list-disc pl-4 space-y-0.5">'); inList = true; }
      result.push(`<li class="text-xs text-gray-700 dark:text-gray-300">${applyInlineFormatting(trimmed.slice(2))}</li>`);
    } else {
      if (inList) { result.push('</ul>'); inList = false; }
      result.push(`<p class="text-xs text-gray-700 dark:text-gray-300 leading-snug">${applyInlineFormatting(trimmed)}</p>`);
    }
  }

  if (inList) result.push('</ul>');
  return result;
}

// ── Component ──────────────────────────────────────────────────────────────

export function TextMarkdownWidget({ config, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.textMarkdown', 'Note');
  const content = config.content ?? '';

  const htmlLines = useMemo(() => parseMarkdown(content), [content]);

  if (!content.trim()) {
    return (
      <div className="h-full flex flex-col items-center justify-center gap-2 p-3">
        <FileText size={18} className="text-gray-300 dark:text-gray-600" />
        <span className="text-xs text-gray-400">
          {t('governance.dashboardView.noContent', 'No content configured. Edit this widget to add a note.')}
        </span>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col gap-1 p-2 overflow-hidden">
      <div className="flex items-center gap-1.5 mb-1">
        <FileText size={13} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>
      <div className="flex-1 overflow-y-auto scrollbar-thin space-y-1">
        {htmlLines.map((html, i) => (
          // Content is produced by our own parser from widget config (no user-submitted raw HTML from external sources)
          // eslint-disable-next-line react/no-danger
          <div key={i} dangerouslySetInnerHTML={{ __html: html }} />
        ))}
      </div>
    </div>
  );
}
