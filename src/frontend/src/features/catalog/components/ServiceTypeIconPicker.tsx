// src/frontend/src/features/catalog/components/ServiceTypeIconPicker.tsx
import { useTranslation } from 'react-i18next';

/** Todos os tipos disponíveis no modo 'service'. */
export const ALL_SERVICE_TYPES = [
  'RestApi', 'GraphqlApi', 'SoapService', 'ZosConnectApi',
  'KafkaProducer', 'KafkaConsumer', 'WebhookProducer', 'WebhookConsumer', 'MqQueue',
  'GrpcService',
  'BackgroundService', 'ScheduledProcess', 'ScheduledJob', 'BackgroundWorker',
  'Gateway',
  'IntegrationComponent', 'SharedPlatformService', 'Framework', 'ThirdParty', 'IntegrationBridge',
  'LegacySystem', 'CobolProgram', 'CicsTransaction', 'ImsTransaction', 'BatchJob', 'MainframeSystem', 'MqQueueManager',
] as const;

export type ServiceType = typeof ALL_SERVICE_TYPES[number];

/** Tipos disponíveis no modo 'interface' (subconjunto de ALL_SERVICE_TYPES). */
const INTERFACE_TYPES: ServiceType[] = [
  'RestApi', 'GraphqlApi', 'SoapService', 'ZosConnectApi',
  'KafkaProducer', 'KafkaConsumer', 'MqQueue',
  'GrpcService',
  'BackgroundWorker', 'ScheduledJob',
  'WebhookProducer', 'WebhookConsumer',
  'Gateway',
];

/** Cor semântica por tipo. */
const TYPE_STYLE: Record<ServiceType, { border: string; bg: string; text: string; stroke: string }> = {
  // API HTTP — índigo
  RestApi:           { border: 'border-indigo-500',  bg: 'bg-indigo-500/10',  text: 'text-indigo-400',  stroke: '#6366f1' },
  GraphqlApi:        { border: 'border-violet-500',  bg: 'bg-violet-500/10',  text: 'text-violet-400',  stroke: '#8b5cf6' },
  SoapService:       { border: 'border-indigo-400',  bg: 'bg-indigo-400/10',  text: 'text-indigo-300',  stroke: '#818cf8' },
  ZosConnectApi:     { border: 'border-indigo-300',  bg: 'bg-indigo-300/10',  text: 'text-indigo-300',  stroke: '#a5b4fc' },
  // Streaming / Eventos — âmbar
  KafkaProducer:     { border: 'border-amber-500',   bg: 'bg-amber-500/10',   text: 'text-amber-400',   stroke: '#f59e0b' },
  KafkaConsumer:     { border: 'border-amber-400',   bg: 'bg-amber-400/10',   text: 'text-amber-300',   stroke: '#fbbf24' },
  WebhookProducer:   { border: 'border-amber-500',   bg: 'bg-amber-500/10',   text: 'text-amber-400',   stroke: '#f59e0b' },
  WebhookConsumer:   { border: 'border-amber-400',   bg: 'bg-amber-400/10',   text: 'text-amber-300',   stroke: '#fbbf24' },
  MqQueue:           { border: 'border-amber-300',   bg: 'bg-amber-300/10',   text: 'text-amber-300',   stroke: '#fcd34d' },
  // RPC — ciano
  GrpcService:       { border: 'border-cyan-500',    bg: 'bg-cyan-500/10',    text: 'text-cyan-400',    stroke: '#06b6d4' },
  // Background — esmeralda
  BackgroundService: { border: 'border-emerald-500', bg: 'bg-emerald-500/10', text: 'text-emerald-400', stroke: '#10b981' },
  ScheduledProcess:  { border: 'border-emerald-400', bg: 'bg-emerald-400/10', text: 'text-emerald-300', stroke: '#34d399' },
  ScheduledJob:      { border: 'border-emerald-400', bg: 'bg-emerald-400/10', text: 'text-emerald-300', stroke: '#34d399' },
  BackgroundWorker:  { border: 'border-emerald-300', bg: 'bg-emerald-300/10', text: 'text-emerald-300', stroke: '#6ee7b7' },
  // Gateway — vermelho
  Gateway:           { border: 'border-red-500',     bg: 'bg-red-500/10',     text: 'text-red-400',     stroke: '#ef4444' },
  // Platform — slate azul
  IntegrationComponent:   { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  SharedPlatformService:  { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  Framework:              { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  ThirdParty:             { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  IntegrationBridge:      { border: 'border-slate-400', bg: 'bg-slate-400/10', text: 'text-slate-300', stroke: '#94a3b8' },
  // Legacy / Mainframe — cinza slate
  LegacySystem:      { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  CobolProgram:      { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  CicsTransaction:   { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  ImsTransaction:    { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  BatchJob:          { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  MainframeSystem:   { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
  MqQueueManager:    { border: 'border-slate-500', bg: 'bg-slate-500/10', text: 'text-slate-400', stroke: '#64748b' },
};

/** Ícone SVG inline 22×22 para cada tipo de serviço. */
function ServiceTypeSvg({ type, stroke }: { type: ServiceType; stroke: string }) {
  switch (type) {
    case 'RestApi':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="2" y="6" width="18" height="10" rx="2" stroke={stroke} strokeWidth="1.5"/>
          <path d="M6 11h4M14 9l2 2-2 2" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'GraphqlApi':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <circle cx="11" cy="11" r="2" stroke={stroke} strokeWidth="1.5"/>
          <circle cx="4" cy="7" r="1.5" stroke={stroke} strokeWidth="1.2"/>
          <circle cx="18" cy="7" r="1.5" stroke={stroke} strokeWidth="1.2"/>
          <circle cx="4" cy="15" r="1.5" stroke={stroke} strokeWidth="1.2"/>
          <circle cx="18" cy="15" r="1.5" stroke={stroke} strokeWidth="1.2"/>
          <path d="M5.3 7.8L9.5 9.5M12.5 9.5l4.2-1.7M5.3 14.2l4.2-1.7M12.5 12.5l4.2 1.7" stroke={stroke} strokeWidth="1.2"/>
        </svg>
      );
    case 'GrpcService':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="8" width="6" height="6" rx="1" stroke={stroke} strokeWidth="1.5"/>
          <rect x="13" y="8" width="6" height="6" rx="1" stroke={stroke} strokeWidth="1.5"/>
          <path d="M9 10l4 0M9 12l4 0" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'KafkaProducer':
    case 'KafkaConsumer':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <circle cx="11" cy="11" r="3" stroke={stroke} strokeWidth="1.5"/>
          <path d="M4 8v-1a2 2 0 012-2h2M18 8v-1a2 2 0 00-2-2h-2M4 14v1a2 2 0 002 2h2M18 14v1a2 2 0 01-2 2h-2" stroke={stroke} strokeWidth="1.2" strokeLinecap="round"/>
        </svg>
      );
    case 'WebhookProducer':
    case 'WebhookConsumer':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <path d="M8 11a3 3 0 106 0" stroke={stroke} strokeWidth="1.5"/>
          <path d="M11 8V5M8 14H5a2 2 0 01-2-2V9M14 14h3a2 2 0 002-2V9" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'Gateway':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <path d="M3 11h16M11 5v12" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
          <circle cx="11" cy="11" r="4" stroke={stroke} strokeWidth="1.5"/>
        </svg>
      );
    case 'BackgroundService':
    case 'BackgroundWorker':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="3" width="16" height="16" rx="3" stroke={stroke} strokeWidth="1.5"/>
          <path d="M7 11l3 3 5-5" stroke={stroke} strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      );
    case 'ScheduledProcess':
    case 'ScheduledJob':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <circle cx="11" cy="11" r="7" stroke={stroke} strokeWidth="1.5"/>
          <path d="M11 7v4l2.5 2.5" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'SoapService':
    case 'ZosConnectApi':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="2" y="5" width="18" height="12" rx="2" stroke={stroke} strokeWidth="1.5"/>
          <path d="M7 9h4M7 13h8" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
    case 'MqQueue':
    case 'MqQueueManager':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="5" width="16" height="4" rx="1" stroke={stroke} strokeWidth="1.3"/>
          <rect x="3" y="9" width="16" height="4" rx="1" stroke={stroke} strokeWidth="1.3"/>
          <rect x="3" y="13" width="16" height="4" rx="1" stroke={stroke} strokeWidth="1.3"/>
        </svg>
      );
    case 'LegacySystem':
    case 'CobolProgram':
    case 'CicsTransaction':
    case 'ImsTransaction':
    case 'BatchJob':
    case 'MainframeSystem':
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="3" width="16" height="16" rx="1" stroke={stroke} strokeWidth="1.5"/>
          <path d="M7 7h8M7 11h8M7 15h4" stroke={stroke} strokeWidth="1.3" strokeLinecap="round"/>
        </svg>
      );
    default:
      return (
        <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
          <rect x="3" y="3" width="16" height="16" rx="3" stroke={stroke} strokeWidth="1.5"/>
          <path d="M11 7v8M7 11h8" stroke={stroke} strokeWidth="1.5" strokeLinecap="round"/>
        </svg>
      );
  }
}

/** Rótulo i18n para um tipo de serviço (fallback para o nome bruto). */
const TYPE_LABEL_KEYS: Record<ServiceType, string> = {
  RestApi: 'serviceCatalog.typeRestApi',
  GraphqlApi: 'serviceCatalog.typeGraphqlApi',
  SoapService: 'serviceCatalog.typeSoapService',
  ZosConnectApi: 'serviceCatalog.typeZosConnectApi',
  KafkaProducer: 'serviceCatalog.typeKafkaProducer',
  KafkaConsumer: 'serviceCatalog.typeKafkaConsumer',
  WebhookProducer: 'serviceCatalog.typeWebhookProducer',
  WebhookConsumer: 'serviceCatalog.typeWebhookConsumer',
  MqQueue: 'serviceCatalog.typeMqQueue',
  GrpcService: 'serviceCatalog.typeGrpcService',
  BackgroundService: 'serviceCatalog.typeBackgroundService',
  ScheduledProcess: 'serviceCatalog.typeScheduledProcess',
  ScheduledJob: 'serviceCatalog.typeScheduledJob',
  BackgroundWorker: 'serviceCatalog.typeBackgroundWorker',
  Gateway: 'serviceCatalog.typeGateway',
  IntegrationComponent: 'serviceCatalog.typeIntegrationComponent',
  SharedPlatformService: 'serviceCatalog.typeSharedPlatformService',
  Framework: 'serviceCatalog.typeFramework',
  ThirdParty: 'serviceCatalog.typeThirdParty',
  IntegrationBridge: 'serviceCatalog.typeIntegrationBridge',
  LegacySystem: 'serviceCatalog.typeLegacySystem',
  CobolProgram: 'serviceCatalog.typeCobolProgram',
  CicsTransaction: 'serviceCatalog.typeCicsTransaction',
  ImsTransaction: 'serviceCatalog.typeImsTransaction',
  BatchJob: 'serviceCatalog.typeBatchJob',
  MainframeSystem: 'serviceCatalog.typeMainframeSystem',
  MqQueueManager: 'serviceCatalog.typeMqQueueManager',
};

interface ServiceTypeIconPickerProps {
  value: string;
  onChange: (type: string) => void;
  /** 'service' mostra todos os tipos; 'interface' mostra apenas tipos de interface */
  mode: 'service' | 'interface';
}

/** Grid de cards selecionáveis com ícone SVG e cor semântica por categoria. */
export function ServiceTypeIconPicker({ value, onChange, mode }: ServiceTypeIconPickerProps) {
  const { t } = useTranslation();
  const types = mode === 'interface' ? INTERFACE_TYPES : [...ALL_SERVICE_TYPES];

  return (
    <div
      role="listbox"
      aria-label={t('serviceCatalog.serviceType', 'Service Type')}
      className="grid gap-2"
      style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(90px, 1fr))' }}
    >
      {types.map((type) => {
        const style = TYPE_STYLE[type];
        const isSelected = value === type;
        return (
          <button
            key={type}
            type="button"
            role="option"
            aria-selected={isSelected}
            onClick={() => onChange(type)}
            className={`flex flex-col items-center gap-1.5 px-2 py-3 rounded-lg border transition-all text-center ${
              isSelected
                ? `${style.border} ${style.bg} ring-1 ring-inset ${style.border}`
                : 'border-edge hover:border-muted bg-canvas/50 hover:bg-canvas'
            }`}
          >
            <ServiceTypeSvg type={type} stroke={isSelected ? style.stroke : '#64748b'} />
            <span
              className={`text-[10px] leading-tight font-medium truncate w-full text-center ${
                isSelected ? style.text : 'text-muted'
              }`}
            >
              {t(TYPE_LABEL_KEYS[type], { defaultValue: type })}
            </span>
          </button>
        );
      })}
    </div>
  );
}
