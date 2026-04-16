import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Search, ChevronDown } from 'lucide-react';
import { changeIntelligenceApi } from '../api/changeIntelligence';

interface ReleaseSelectorProps {
  /** ID da release actualmente seleccionada. */
  value: string;
  /** Callback chamado quando o utilizador selecciona uma release. */
  onChange: (releaseId: string, serviceName: string, version: string) => void;
  /** Placeholder exibido enquanto nenhuma release está seleccionada. */
  placeholder?: string;
}

/**
 * Componente reutilizável de selecção de release.
 *
 * Substitui o anti-padrão de input de UUID manual por um dropdown que lista
 * as releases recentes carregadas a partir do endpoint paginado.
 * Respeita as regras de produto: o utilizador final nunca deve introduzir GUIDs.
 */
export function ReleaseSelector({ value, onChange, placeholder }: ReleaseSelectorProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['releases-selector'],
    queryFn: () => changeIntelligenceApi.listRecentReleases(1, 50),
    staleTime: 60_000,
  });

  const releases = data?.items ?? [];
  const filtered = releases.filter(
    (r) =>
      r.serviceName.toLowerCase().includes(search.toLowerCase()) ||
      r.version.toLowerCase().includes(search.toLowerCase()) ||
      r.environment.toLowerCase().includes(search.toLowerCase()),
  );

  const selected = releases.find((r) => r.id === value);

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="w-full flex items-center justify-between gap-2 rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
      >
        {selected ? (
          <span>
            <span className="font-medium">{selected.serviceName}</span>
            <span className="text-muted ml-2">
              {selected.version} — {selected.environment}
            </span>
          </span>
        ) : (
          <span className="text-muted">
            {placeholder ?? t('releaseSelector.placeholder', 'Select a release…')}
          </span>
        )}
        <ChevronDown size={14} className="text-muted flex-shrink-0" />
      </button>

      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-md border border-edge bg-surface shadow-lg">
          {/* Search box */}
          <div className="flex items-center gap-2 px-3 py-2 border-b border-edge">
            <Search size={14} className="text-muted flex-shrink-0" />
            <input
              autoFocus
              className="flex-1 bg-transparent text-sm text-heading placeholder:text-muted focus:outline-none"
              placeholder={t('releaseSelector.search', 'Filter by service, version or environment…')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          {/* Options */}
          <ul className="max-h-60 overflow-y-auto py-1">
            {isLoading && (
              <li className="px-3 py-2 text-sm text-muted">
                {t('releaseSelector.loading', 'Loading releases…')}
              </li>
            )}
            {!isLoading && filtered.length === 0 && (
              <li className="px-3 py-2 text-sm text-muted">
                {t('releaseSelector.noResults', 'No releases found')}
              </li>
            )}
            {filtered.map((r) => (
              <li key={r.id}>
                <button
                  type="button"
                  className="w-full text-left px-3 py-2 text-sm hover:bg-canvas transition-colors"
                  onClick={() => {
                    onChange(r.id, r.serviceName, r.version);
                    setOpen(false);
                    setSearch('');
                  }}
                >
                  <div className="font-medium text-heading">{r.serviceName}</div>
                  <div className="text-xs text-muted">
                    {r.version} — {r.environment} — {r.status}
                  </div>
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
