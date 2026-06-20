import { useTranslation } from 'react-i18next';
import { Check } from 'lucide-react';
import { SearchInput } from '../../../../shared/ui';
import { supportsContracts, allowedContractTypes } from '../../shared/serviceContractPolicy';
import type { ServiceType } from '../../../../types';

interface ServiceItem {
  serviceId: string;
  displayName: string;
  domain: string;
  teamName: string;
  serviceType: string;
}

interface ServiceTabProps {
  filteredServices: ServiceItem[];
  linkedServiceId: string;
  serviceSearch: string;
  isLoading: boolean;
  onSearchChange: (v: string) => void;
  onSelectService: (svc: { serviceId: string; serviceType: string }) => void;
}

/**
 * Galeria de serviços do catálogo para vincular ao contrato.
 * Componente presentacional: pesquisa e selecção via callbacks.
 */
export function ServiceTab({
  filteredServices,
  linkedServiceId,
  serviceSearch,
  isLoading,
  onSearchChange,
  onSelectService,
}: ServiceTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-base font-semibold text-heading mb-1">
          {t('contracts.create.selectService', 'Select the service for this contract')}
        </h2>
        <p className="text-xs text-muted">
          {t(
            'contracts.create.linkedServiceHint',
            'Contracts are always bound to a catalog service. Link the right service to enable publishing and workspace enrichment.',
          )}
        </p>
      </div>

      {/* Search */}
      <SearchInput
        size="sm"
        value={serviceSearch}
        onChange={(e) => onSearchChange(e.target.value)}
        placeholder={t('contracts.create.searchServices', 'Search by name, domain or team...')}
      />

      {/* Service cards */}
      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-[76px] rounded-lg bg-elevated animate-pulse border border-edge" />
          ))}
        </div>
      ) : filteredServices.length === 0 ? (
        <div className="py-10 text-center text-sm text-muted border border-edge rounded-lg bg-elevated/20">
          {serviceSearch
            ? t('contracts.create.noServicesMatch', 'No services match your search')
            : t('contracts.create.noServices', 'No services available')}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 max-h-[460px] overflow-y-auto pr-1">
          {filteredServices.map((svc) => {
            const isSelected = linkedServiceId === svc.serviceId;
            const svcType = svc.serviceType as ServiceType;
            const supported = supportsContracts(svcType);
            const typeCount = allowedContractTypes(svcType).length;

            return (
              <button
                key={svc.serviceId}
                type="button"
                onClick={() => {
                  if (!supported) return;
                  onSelectService({ serviceId: svc.serviceId, serviceType: svc.serviceType });
                }}
                disabled={!supported}
                className={`text-left rounded-lg border p-3.5 transition-all
                  ${isSelected
                    ? 'border-accent bg-accent/5 ring-1 ring-accent/20'
                    : !supported
                      ? 'border-edge bg-elevated/20 opacity-50 cursor-not-allowed'
                      : 'border-edge bg-card hover:border-accent/30 hover:bg-elevated/30'}`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      {isSelected && (
                        <span className="w-4 h-4 rounded-full bg-accent flex items-center justify-center shrink-0">
                          <Check size={9} className="text-white" />
                        </span>
                      )}
                      <span className="text-sm font-semibold text-heading truncate">{svc.displayName}</span>
                    </div>
                    <p className="text-xs text-muted mt-0.5 truncate">
                      {svc.teamName} · {svc.domain}
                    </p>
                  </div>
                  <span className="text-[10px] font-medium px-1.5 py-0.5 rounded bg-elevated text-muted border border-edge shrink-0 whitespace-nowrap">
                    {svc.serviceType}
                  </span>
                </div>

                <p className={`text-[10px] mt-2 ${supported ? 'text-muted/70' : 'text-warning/80'}`}>
                  {supported
                    ? typeCount === 1
                      ? t('contracts.create.oneTypeAvailable', '1 contract type available')
                      : t('contracts.create.nTypesAvailable', '{{n}} contract types available', {
                          n: typeCount,
                        })
                    : t('contracts.create.noTypesAvailable', 'No contract types for this service')}
                </p>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
