import { useTranslation } from 'react-i18next';
import { Globe } from 'lucide-react';
import { PROTOCOL_BY_TYPE, PROTOCOL_COLORS, type ContractTypeValue } from '../../shared/constants';
import { TYPE_ICONS, BEST_FOR_KEY, CREATION_MODES, type CreationMode } from '../contractCreateConstants';

interface TypeModeTabProps {
  filteredContractTypes: ReadonlyArray<{ value: string; labelKey: string }>;
  selectedType: ContractTypeValue | null;
  onSelectType: (t: ContractTypeValue) => void;
  selectedMode: CreationMode | null;
  onSelectMode: (m: CreationMode) => void;
}

/** Rótulos default (inglês) por tipo — usados como fallback de i18n. */
const TYPE_LABEL_DEFAULT: Record<string, string> = {
  RestApi: 'REST / OpenAPI',
  Soap: 'SOAP / WSDL',
  Event: 'Event / AsyncAPI',
  BackgroundService: 'Background Service',
  SharedSchema: 'Shared Schema',
  Copybook: 'Copybook',
  MqMessage: 'MQ Message',
  FixedLayout: 'Fixed Layout',
  CicsCommarea: 'CICS Commarea',
  Webhook: 'Webhook',
};

/** Linha "Best for" default (inglês) por tipo — usada como fallback de i18n. */
const BEST_FOR_DEFAULT: Record<string, string> = {
  RestApi: 'Best for HTTP request/response APIs between microservices',
  Event: 'Best for Kafka, AMQP, SNS, WebSocket event-driven services',
  Soap: 'Best for legacy SOAP services and enterprise integrations',
  SharedSchema: 'Reusable types referenced across multiple contracts',
};

/**
 * Galeria de tipos de contrato + galeria de modos de criação.
 * Componente presentacional: reporta selecção via callbacks, sem estado próprio.
 */
export function TypeModeTab({
  filteredContractTypes,
  selectedType,
  onSelectType,
  selectedMode,
  onSelectMode,
}: TypeModeTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      {/* ── Contract type ─── */}
      <div>
        <h2 className="text-sm font-semibold text-heading mb-0.5">
          {t('contracts.create.selectType', 'What type of contract?')}
        </h2>
        <p className="text-xs text-muted mb-4">
          {t(
            'contracts.create.selectTypeHint',
            "Choose the contract type that matches this service's interface style.",
          )}
        </p>

        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
          {filteredContractTypes.map((ct) => {
            const Icon = TYPE_ICONS[ct.value] ?? Globe;
            const isSelected = selectedType === ct.value;
            const ctProtocols = PROTOCOL_BY_TYPE[ct.value as ContractTypeValue] ?? [];
            const bestFor = t(BEST_FOR_KEY(ct.value), BEST_FOR_DEFAULT[ct.value] ?? '');

            return (
              <button
                key={ct.value}
                type="button"
                onClick={() => onSelectType(ct.value as ContractTypeValue)}
                className={`text-left rounded-xl border p-4 transition-all
                  ${isSelected
                    ? 'border-accent bg-accent/5 ring-1 ring-accent/30'
                    : 'border-edge bg-card hover:border-accent/30 hover:bg-elevated/30'}`}
              >
                <div
                  className={`w-9 h-9 rounded-lg flex items-center justify-center mb-3
                    ${isSelected ? 'bg-accent/20 text-accent' : 'bg-elevated text-muted'}`}
                >
                  <Icon size={18} />
                </div>
                <p className={`text-sm font-semibold mb-1 ${isSelected ? 'text-accent' : 'text-heading'}`}>
                  {t(ct.labelKey, TYPE_LABEL_DEFAULT[ct.value] ?? ct.value)}
                </p>
                {bestFor && <p className="text-[11px] text-muted leading-relaxed mb-2">{bestFor}</p>}
                {ctProtocols.length > 0 ? (
                  <div className="flex flex-wrap gap-1">
                    {ctProtocols.map((p) => (
                      <span
                        key={p}
                        className={`text-[10px] px-1.5 py-0.5 rounded font-medium ${PROTOCOL_COLORS[p] ?? 'bg-muted/15 text-muted border border-muted/25'}`}
                      >
                        {p}
                      </span>
                    ))}
                  </div>
                ) : (
                  <span className="text-[10px] text-muted/60 italic">
                    {t('contracts.create.noProtocol', 'No protocol')}
                  </span>
                )}
              </button>
            );
          })}
        </div>
      </div>

      {/* ── Creation mode — appears after type is selected ─── */}
      {selectedType && (
        <div>
          <div className="h-px bg-edge mb-6" />
          <h2 className="text-sm font-semibold text-heading mb-0.5">
            {t('contracts.create.selectMode', 'How do you want to create it?')}
          </h2>
          <p className="text-xs text-muted mb-4">
            {t(
              'contracts.create.selectModeHint',
              "Choose how you'd like to define the contract specification.",
            )}
          </p>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            {CREATION_MODES.map((mode) => {
              const isSelected = selectedMode === mode.id;
              return (
                <button
                  key={mode.id}
                  type="button"
                  onClick={() => onSelectMode(mode.id)}
                  className={`text-left rounded-xl border p-4 transition-all
                    ${isSelected
                      ? 'border-accent bg-accent/5 ring-1 ring-accent/30'
                      : 'border-edge bg-card hover:border-accent/30 hover:bg-elevated/30'}`}
                >
                  <div
                    className={`w-9 h-9 rounded-lg flex items-center justify-center mb-3
                      ${isSelected ? 'bg-accent/20 text-accent' : 'bg-elevated text-muted'}`}
                  >
                    <mode.Icon size={18} />
                  </div>
                  <p className={`text-sm font-semibold mb-1 ${isSelected ? 'text-accent' : 'text-heading'}`}>
                    {t(mode.labelKey, mode.id)}
                  </p>
                  <p className="text-xs text-muted leading-relaxed">
                    {t(mode.descriptionKey, `Create using ${mode.id}`)}
                  </p>
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
