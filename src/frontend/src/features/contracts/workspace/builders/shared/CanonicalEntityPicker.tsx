/**
 * CanonicalEntityPicker — modal para selecionar entidades canónicas do catálogo.
 * Utilizado no SchemaPropertyEditor para o campo $ref.
 */

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { CheckCircle2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { contractsApi } from '../../../api/contracts';
import { Modal, SearchInput } from '../../../../../shared/ui';

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
    <Modal
      open
      onClose={onClose}
      title={t('contracts.builder.canonical.picker.title', 'Browse Canonical Entities')}
      size="lg"
    >
      <div className="flex flex-col gap-3">
        {/* Search */}
        <SearchInput
          size="sm"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t('contracts.builder.canonical.picker.search', 'Search entities...')}
          aria-label={t('contracts.builder.canonical.picker.search', 'Search entities...')}
        />

        {/* List */}
        <div className="max-h-[60vh] overflow-y-auto -mx-1">
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
                  className="flex items-center justify-between px-2 py-2.5 hover:bg-elevated/50 transition-colors"
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
                    <CheckCircle2 size={12} />
                    {t('contracts.builder.canonical.picker.select', 'Select')}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </Modal>
  );
}
