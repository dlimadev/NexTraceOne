import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { ClipboardList, ArrowRight } from 'lucide-react';
import { serviceCatalogApi } from '../api';

/** Serviços ainda por configurar (Planning). Honest-null: oculta-se quando não há nenhum. */
export function ServicesNeedingSetupSection() {
  const { t } = useTranslation();
  const { data } = useQuery({
    queryKey: ['catalog-services-needing-setup'],
    queryFn: () => serviceCatalogApi.listServices({ lifecycleStatus: 'Planning', pageSize: 5 }),
    staleTime: 30_000,
  });

  const items = data?.items ?? [];
  if (items.length === 0) return null;

  return (
    <section className="mb-6">
      <h2 className="flex items-center gap-2 text-sm font-semibold text-heading mb-3">
        <ClipboardList size={16} />
        {t('selfServicePortal.needingSetup.title')}
      </h2>
      <ul className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        {items.slice(0, 5).map((s) => (
          <li key={s.serviceId}>
            <Link
              to={`/services/${s.serviceId}`}
              className="group flex items-center gap-2 rounded-lg border border-edge bg-card px-3 py-2.5 text-sm shadow-sm transition-all hover:border-accent/40"
            >
              <span className="min-w-0 truncate text-heading group-hover:text-accent">
                {t('selfServicePortal.needingSetup.item', { name: s.displayName, status: s.lifecycleStatus })}
              </span>
              <ArrowRight size={13} className="ml-auto shrink-0 text-muted group-hover:text-accent" />
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
