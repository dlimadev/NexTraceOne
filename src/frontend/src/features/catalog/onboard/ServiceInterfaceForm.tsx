import { useTranslation } from 'react-i18next';
import { TextField, TextArea, Select, Checkbox } from '../../../shared/ui';
import {
  type ServiceInterfaceValues,
  INTERFACE_TYPE_OPTIONS,
  INTERFACE_EXPOSURE_OPTIONS,
  type SelectOptionKey,
} from './onboardValidation';

interface ServiceInterfaceFormProps {
  values: ServiceInterfaceValues;
  errors: Partial<Record<keyof ServiceInterfaceValues, string>>;
  onChange: <K extends keyof ServiceInterfaceValues>(key: K, value: ServiceInterfaceValues[K]) => void;
}

const TYPES_WITH_BASE_PATH = ['RestApi', 'SoapService', 'ZosConnectApi', 'GraphqlApi'];
const TYPES_WITH_WSDL = ['SoapService'];
const TYPES_WITH_TOPIC = ['KafkaProducer', 'KafkaConsumer', 'MqQueue'];
const TYPES_WITH_GRPC = ['GrpcService'];
const TYPES_WITH_CRON = ['BackgroundWorker', 'ScheduledJob'];

/** Formulário controlado de interface de serviço — partilhado pelo onboarding e pela página autónoma. */
export function ServiceInterfaceForm({ values, errors, onChange }: ServiceInterfaceFormProps) {
  const { t } = useTranslation();
  const opts = (list: SelectOptionKey[]) => list.map((o) => ({ value: o.value, label: t(o.labelKey) }));

  const showBasePath = TYPES_WITH_BASE_PATH.includes(values.interfaceType);
  const showWsdl = TYPES_WITH_WSDL.includes(values.interfaceType);
  const showTopic = TYPES_WITH_TOPIC.includes(values.interfaceType);
  const showGrpc = TYPES_WITH_GRPC.includes(values.interfaceType);
  const showCron = TYPES_WITH_CRON.includes(values.interfaceType);

  return (
    <div className="space-y-5 max-w-2xl">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <TextField
          label={t('serviceInterfaces.fieldName', 'Interface Name')}
          value={values.name}
          onChange={(e) => onChange('name', e.target.value)}
          placeholder={t('serviceInterfaces.fieldNamePlaceholder', 'Orders REST API v1')}
          required
          error={errors.name}
          size="sm"
        />
        <Select
          label={t('serviceInterfaces.fieldType', 'Interface Type')}
          value={values.interfaceType}
          onChange={(e) => onChange('interfaceType', e.target.value)}
          options={opts(INTERFACE_TYPE_OPTIONS)}
          size="sm"
        />
      </div>

      <TextArea
        label={t('serviceInterfaces.fieldDescription', 'Description')}
        value={values.description}
        onChange={(e) => onChange('description', e.target.value)}
        rows={3}
      />

      <Select
        label={t('serviceInterfaces.fieldExposure', 'Exposure Scope')}
        value={values.exposureScope}
        onChange={(e) => onChange('exposureScope', e.target.value)}
        options={opts(INTERFACE_EXPOSURE_OPTIONS)}
        size="sm"
      />

      {showBasePath && (
        <TextField label={t('serviceInterfaces.fieldBasePath', 'Base Path')} value={values.basePath}
          onChange={(e) => onChange('basePath', e.target.value)}
          placeholder={t('serviceInterfaces.fieldBasePathPlaceholder', '/api/orders')} className="font-mono" size="sm" />
      )}
      {showWsdl && (
        <TextField label={t('serviceInterfaces.fieldWsdlNamespace', 'WSDL Namespace')} value={values.wsdlNamespace}
          onChange={(e) => onChange('wsdlNamespace', e.target.value)} className="font-mono" size="sm" />
      )}
      {showTopic && (
        <TextField label={t('serviceInterfaces.fieldTopicName', 'Topic Name')} value={values.topicName}
          onChange={(e) => onChange('topicName', e.target.value)}
          placeholder={t('serviceInterfaces.fieldTopicNamePlaceholder', 'orders.events.v1')} className="font-mono" size="sm" />
      )}
      {showGrpc && (
        <TextField label={t('serviceInterfaces.fieldGrpcServiceName', 'Proto Service Name')} value={values.grpcServiceName}
          onChange={(e) => onChange('grpcServiceName', e.target.value)} className="font-mono" size="sm" />
      )}
      {showCron && (
        <TextField label={t('serviceInterfaces.fieldScheduleCron', 'Cron Expression')} value={values.scheduleCron}
          onChange={(e) => onChange('scheduleCron', e.target.value)} placeholder="0 */5 * * *" className="font-mono" size="sm" />
      )}

      <TextField
        label={t('serviceInterfaces.fieldDocumentationUrl', 'Documentation URL')}
        type="url"
        value={values.documentationUrl}
        onChange={(e) => onChange('documentationUrl', e.target.value)}
        className="font-mono"
        size="sm"
      />

      <Checkbox
        id="requires-contract"
        checked={values.requiresContract}
        onChange={(e) => onChange('requiresContract', e.target.checked)}
        label={t('serviceInterfaces.fieldRequiresContract', 'Requires Contract')}
      />
    </div>
  );
}
