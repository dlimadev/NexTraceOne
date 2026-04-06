/**
 * CanonicalEntityPicker — modal para selecionar entidades canónicas do catálogo.
 * Utilizado no SchemaPropertyEditor para o campo $ref.
 */

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Search, X, CheckCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { contractsApi } from '../../../api/contracts';

interface CanonicalEntityPickerProps {
  onSelect: (ref: string) => void;
  onClose: () => void;
}

export function CanonicalEntityPicker({ onSelect, onClose }: CanonicalEntityPickerProps) {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['canonical-entities-picker', search],
    queryFn: () =>
      contractsApi.listCanonicalEntities({ searchTerm: search || undefined }),
    staleTime: 30_000,
  });

  const entities = data?.items ?? [];

  const handleSelect = (name: string) => {
    onSelect(`#/components/schemas/${name}`);
    onClose();
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
      role="dialog"
      aria-modal="true"
      aria-label={t('contracts.builder.canonical.picker.title', 'Browse Canonical Entities')}
    >
      <div className="bg-panel border border-edge rounded-lg shadow-xl w-full max-w-lg mx-4 flex flex-col max-h-[80vh]">
        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-edge">
          <h2 className="text-sm font-semibold text-heading">
            {t('contracts.builder.canonical.picker.title', 'Browse Canonical Entities')}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="text-muted hover:text-body transition-colors"
            aria-label={t('common.close', 'Close')}
          >
            <X size={16} />
          </button>
        </div>

        {/* Search */}
        <div className="px-4 py-2 border-b border-edge">
          <div className="relative">
            <Search size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted/50" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('contracts.builder.canonical.picker.search', 'Search entities...')}
              className="w-full text-xs bg-elevated border border-edge rounded pl-7 pr-3 py-1.5 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
              autoFocus
            />
          </div>
        </div>

        {/* List */}
        <div className="flex-1 overflow-y-auto">
          {isLoading ? (
            <div className="py-8 text-center text-xs text-muted">{t('common.loading', 'Loading...')}</div>
          ) : entities.length === 0 ? (
            <div className="py-8 text-center text-xs text-muted">
              {t('contracts.builder.canonical.picker.empty', 'No canonical entities found')}
            </div>
          ) : (
            <ul className="divide-y divide-edge">
              {entities.map((entity) => (
                <li
                  key={entity.id}
                  className="flex items-center justify-between px-4 py-2.5 hover:bg-elevated/50 transition-colors"
                >
                  <div className="min-w-0">
                    <p className="text-xs font-medium text-heading truncate">{entity.name}</p>
                    <p className="text-[10px] text-muted truncate">
                      {[entity.domain, entity.category].filter(Boolean).join(' · ')}
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleSelect(entity.name)}
                    className="ml-3 flex-shrink-0 flex items-center gap-1 text-[10px] font-medium text-accent hover:text-accent/80 transition-colors"
                  >
                    <CheckCircle size={12} />
                    {t('contracts.builder.canonical.picker.select', 'Select')}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
