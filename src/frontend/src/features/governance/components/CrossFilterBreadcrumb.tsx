import { useTranslation } from 'react-i18next';
import { useCrossFilter, type CrossFilter } from '../context/CrossFilterContext';

// ── Component ─────────────────────────────────────────────────────────────

export function CrossFilterBreadcrumb() {
  const { t } = useTranslation();
  const { filter, hasFilter, clearFilter, patchFilter } = useCrossFilter();

  if (!hasFilter) return null;

  const chips = buildChips(filter, t);

  return (
    <div
      role="status"
      aria-label={t('crossFilter.activeFilters')}
      className="flex flex-wrap items-center gap-1.5 rounded-md border border-blue-800 bg-blue-950/60 px-3 py-1.5"
    >
      <span className="text-xs font-medium text-blue-300 shrink-0">
        {t('crossFilter.activeFilters')}:
      </span>

      {chips.map(chip => (
        <span
          key={chip.key}
          className="flex items-center gap-1 rounded-full border border-blue-700 bg-blue-900 px-2 py-0.5 text-xs text-blue-200"
        >
          <span className="font-medium text-blue-400">{chip.label}:</span>
          <span>{chip.value}</span>
          <button
            type="button"
            onClick={() => patchFilter({ [chip.key]: null })}
            className="ml-0.5 text-blue-400 hover:text-blue-200 leading-none"
            aria-label={t('crossFilter.removeFilter', { label: chip.label })}
          >
            ✕
          </button>
        </span>
      ))}

      <button
        type="button"
        onClick={clearFilter}
        className="ml-auto text-xs text-blue-400 hover:text-blue-200 shrink-0"
        aria-label={t('crossFilter.clearAll')}
      >
        {t('crossFilter.clearAll')}
      </button>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────

interface Chip {
  key: keyof CrossFilter;
  label: string;
  value: string;
}

function buildChips(filter: CrossFilter, t: (key: string) => string): Chip[] {
  const chips: Chip[] = [];
  if (filter.serviceId) chips.push({ key: 'serviceId', label: t('crossFilter.service'), value: filter.serviceId });
  if (filter.teamId)    chips.push({ key: 'teamId',    label: t('crossFilter.team'),    value: filter.teamId });
  if (filter.from)      chips.push({ key: 'from',      label: t('crossFilter.from'),    value: new Date(filter.from).toLocaleString() });
  if (filter.to)        chips.push({ key: 'to',        label: t('crossFilter.to'),      value: new Date(filter.to).toLocaleString() });
  return chips;
}
