/**
 * Página de criação de uma nova interface de exposição de serviço.
 * Pertence ao módulo Service Catalog — bounded context de Service Interfaces.
 */
import { useState, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Layers, Check } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { serviceCatalogApi } from '../api';
import type { InterfaceType, ExposureType } from '../../../types';

const inputClass =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';
const selectClass = inputClass;
const monoClass = `${inputClass} font-mono`;
const textareaClass = `${inputClass} resize-none`;

interface CreateInterfaceFormData {
  name: string;
  interfaceType: InterfaceType;
  description: string;
  exposureScope: ExposureType;
  basePath: string;
  topicName: string;
  wsdlNamespace: string;
  grpcServiceName: string;
  scheduleCron: string;
  documentationUrl: string;
  requiresContract: boolean;
}

const INITIAL_FORM: CreateInterfaceFormData = {
  name: '',
  interfaceType: 'RestApi',
  description: '',
  exposureScope: 'Internal',
  basePath: '',
  topicName: '',
  wsdlNamespace: '',
  grpcServiceName: '',
  scheduleCron: '',
  documentationUrl: '',
  requiresContract: false,
};

/** Tipos de interface que expõem um BasePath. */
const TYPES_WITH_BASE_PATH: InterfaceType[] = ['RestApi', 'SoapService', 'ZosConnectApi', 'GraphqlApi'];
/** Tipos com WsdlNamespace. */
const TYPES_WITH_WSDL: InterfaceType[] = ['SoapService'];
/** Tipos com TopicName. */
const TYPES_WITH_TOPIC: InterfaceType[] = ['KafkaProducer', 'KafkaConsumer', 'MqQueue'];
/** Tipos com GrpcServiceName. */
const TYPES_WITH_GRPC: InterfaceType[] = ['GrpcService'];
/** Tipos com ScheduleCron. */
const TYPES_WITH_CRON: InterfaceType[] = ['BackgroundWorker', 'ScheduledJob'];

/** Página de criação de interface de serviço. */
export function CreateServiceInterfacePage() {
  const { t } = useTranslation();
  const { serviceId } = useParams<{ serviceId: string }>();
  const navigate = useNavigate();

  const [form, setForm] = useState<CreateInterfaceFormData>(INITIAL_FORM);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const { data: service, isLoading: isLoadingService, isError: isServiceError } = useQuery({
    queryKey: ['catalog-service-detail', serviceId],
    queryFn: () => serviceCatalogApi.getServiceDetail(serviceId!),
    enabled: !!serviceId,
  });

  const mutation = useMutation({
    mutationFn: () =>
      serviceCatalogApi.createServiceInterface({
        serviceAssetId: serviceId!,
        name: form.name,
        interfaceType: form.interfaceType,
        description: form.description || undefined,
        exposureScope: form.exposureScope,
        basePath: form.basePath || undefined,
        topicName: form.topicName || undefined,
        wsdlNamespace: form.wsdlNamespace || undefined,
        grpcServiceName: form.grpcServiceName || undefined,
        scheduleCron: form.scheduleCron || undefined,
        documentationUrl: form.documentationUrl || undefined,
        requiresContract: form.requiresContract,
      }),
    onSuccess: () => {
      navigate(`/services/${serviceId}`);
    },
  });

  const set = useCallback(<K extends keyof CreateInterfaceFormData>(
    field: K,
    value: CreateInterfaceFormData[K],
  ) => {
    setForm((f) => ({ ...f, [field]: value }));
    setErrors((e) => ({ ...e, [field]: '' }));
  }, []);

  const validate = useCallback((): boolean => {
    const errs: Record<string, string> = {};
    if (!form.name.trim()) {
      errs.name = t('serviceInterfaces.fieldName', 'Interface Name') + ' ' + t('validation.required', 'is required');
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }, [form, t]);

  const handleSubmit = useCallback(() => {
    if (validate()) {
      mutation.mutate();
    }
  }, [validate, mutation]);

  if (isLoadingService) {
    return (
      <PageContainer>
        <PageLoadingState size="lg" />
      </PageContainer>
    );
  }

  if (isServiceError || !service) {
    return (
      <PageContainer>
        <PageErrorState message={t('common.noResults')} />
      </PageContainer>
    );
  }

  const showBasePath = TYPES_WITH_BASE_PATH.includes(form.interfaceType);
  const showWsdl = TYPES_WITH_WSDL.includes(form.interfaceType);
  const showTopic = TYPES_WITH_TOPIC.includes(form.interfaceType);
  const showGrpc = TYPES_WITH_GRPC.includes(form.interfaceType);
  const showCron = TYPES_WITH_CRON.includes(form.interfaceType);

  return (
    <PageContainer className="animate-fade-in">
      {/* ── Breadcrumb / Back ── */}
      <div className="mb-4">
        <Link
          to={`/services/${serviceId}`}
          className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors"
        >
          <ArrowLeft size={14} />
          {service.displayName || service.name}
        </Link>
      </div>

      {/* ── Page header ── */}
      <div className="flex items-center gap-3 mb-6">
        <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-accent/10">
          <Layers size={20} className="text-accent" aria-hidden="true" />
        </div>
        <div>
          <h1 className="text-xl font-semibold text-heading">
            {t('serviceInterfaces.createTitle', 'New Interface')}
          </h1>
          <p className="text-sm text-muted">
            {t('serviceInterfaces.createSubtitle', 'Register a new interface for this service')}
            {' — '}
            <span className="text-body font-medium">{service.displayName || service.name}</span>
          </p>
        </div>
      </div>

      <PageSection>
        <Card>
          <CardHeader>
            <h2 className="text-base font-semibold text-heading">
              {t('serviceInterfaces.createTitle', 'New Interface')}
            </h2>
          </CardHeader>
          <CardBody>
            <div className="space-y-6 max-w-2xl">

              {/* Name + Type */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('serviceInterfaces.fieldName', 'Interface Name')} <span className="text-danger">*</span>
                  </label>
                  <input
                    type="text"
                    value={form.name}
                    onChange={(e) => set('name', e.target.value)}
                    placeholder={t('serviceInterfaces.fieldNamePlaceholder', 'Orders REST API v1')}
                    className={inputClass}
                    autoFocus
                  />
                  {errors.name && <p className="text-xs text-danger mt-1">{errors.name}</p>}
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('serviceInterfaces.fieldType', 'Interface Type')}
                  </label>
                  <select
                    value={form.interfaceType}
                    onChange={(e) => set('interfaceType', e.target.value as InterfaceType)}
                    className={selectClass}
                  >
                    <optgroup label={t('serviceInterfaces.groupPublicApi', 'Public API')}>
                      <option value="RestApi">{t('serviceInterfaces.typeRestApi', 'REST API')}</option>
                      <option value="GraphqlApi">{t('serviceInterfaces.typeGraphqlApi', 'GraphQL API')}</option>
                      <option value="GrpcService">{t('serviceInterfaces.typeGrpcService', 'gRPC Service')}</option>
                      <option value="SoapService">{t('serviceInterfaces.typeSoapService', 'SOAP Service')}</option>
                      <option value="ZosConnectApi">{t('serviceInterfaces.typeZosConnectApi', 'z/OS Connect API')}</option>
                    </optgroup>
                    <optgroup label={t('serviceInterfaces.groupEventDriven', 'Event-Driven')}>
                      <option value="KafkaProducer">{t('serviceInterfaces.typeKafkaProducer', 'Kafka Producer')}</option>
                      <option value="KafkaConsumer">{t('serviceInterfaces.typeKafkaConsumer', 'Kafka Consumer')}</option>
                      <option value="WebhookProducer">{t('serviceInterfaces.typeWebhookProducer', 'Webhook Producer')}</option>
                      <option value="WebhookConsumer">{t('serviceInterfaces.typeWebhookConsumer', 'Webhook Consumer')}</option>
                      <option value="MqQueue">{t('serviceInterfaces.typeMqQueue', 'MQ Queue')}</option>
                    </optgroup>
                    <optgroup label={t('serviceInterfaces.groupBackground', 'Background')}>
                      <option value="BackgroundWorker">{t('serviceInterfaces.typeBackgroundWorker', 'Background Worker')}</option>
                      <option value="ScheduledJob">{t('serviceInterfaces.typeScheduledJob', 'Scheduled Job')}</option>
                    </optgroup>
                    <optgroup label={t('serviceInterfaces.groupInfrastructure', 'Infrastructure')}>
                      <option value="IntegrationBridge">{t('serviceInterfaces.typeIntegrationBridge', 'Integration Bridge')}</option>
                    </optgroup>
                  </select>
                </div>
              </div>

              {/* Description */}
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('serviceInterfaces.fieldDescription', 'Description')}
                </label>
                <textarea
                  value={form.description}
                  onChange={(e) => set('description', e.target.value)}
                  rows={3}
                  className={textareaClass}
                />
              </div>

              {/* Exposure */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('serviceInterfaces.fieldExposure', 'Exposure Scope')}
                  </label>
                  <select
                    value={form.exposureScope}
                    onChange={(e) => set('exposureScope', e.target.value as ExposureType)}
                    className={selectClass}
                  >
                    <option value="Internal">{t('catalog.badges.exposure.Internal', 'Internal')}</option>
                    <option value="Partner">{t('catalog.badges.exposure.Partner', 'Partner')}</option>
                    <option value="External">{t('catalog.badges.exposure.External', 'External')}</option>
                  </select>
                </div>
              </div>

              {/* Conditional fields */}
              {showBasePath && (
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('serviceInterfaces.fieldBasePath', 'Base Path')}
                  </label>
                  <input
                    type="text"
                    value={form.basePath}
                    onChange={(e) => set('basePath', e.target.value)}
                    placeholder={t('serviceInterfaces.fieldBasePathPlaceholder', '/api/orders')}
                    className={monoClass}
                  />
                </div>
              )}

              {showWsdl && (
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('serviceInterfaces.fieldWsdlNamespace', 'WSDL Namespace')}
                  </label>
                  <input
                    type="text"
                    value={form.wsdlNamespace}
                    onChange={(e) => set('wsdlNamespace', e.target.value)}
                    className={monoClass}
                  />
                </div>
              )}

              {showTopic && (
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('serviceInterfaces.fieldTopicName', 'Topic Name')}
                  </label>
                  <input
                    type="text"
                    value={form.topicName}
                    onChange={(e) => set('topicName', e.target.value)}
                    placeholder={t('serviceInterfaces.fieldTopicNamePlaceholder', 'orders.events.v1')}
                    className={monoClass}
                  />
                </div>
              )}

              {showGrpc && (
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('serviceInterfaces.fieldGrpcServiceName', 'Proto Service Name')}
                  </label>
                  <input
                    type="text"
                    value={form.grpcServiceName}
                    onChange={(e) => set('grpcServiceName', e.target.value)}
                    className={monoClass}
                  />
                </div>
              )}

              {showCron && (
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('serviceInterfaces.fieldScheduleCron', 'Cron Expression')}
                  </label>
                  <input
                    type="text"
                    value={form.scheduleCron}
                    onChange={(e) => set('scheduleCron', e.target.value)}
                    placeholder="0 */5 * * *"
                    className={monoClass}
                  />
                </div>
              )}

              {/* Documentation URL */}
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('serviceInterfaces.fieldDocumentationUrl', 'Documentation URL')}
                </label>
                <input
                  type="url"
                  value={form.documentationUrl}
                  onChange={(e) => set('documentationUrl', e.target.value)}
                  className={monoClass}
                />
              </div>

              {/* Requires Contract */}
              <div className="flex items-center gap-3">
                <input
                  id="requires-contract"
                  type="checkbox"
                  checked={form.requiresContract}
                  onChange={(e) => set('requiresContract', e.target.checked)}
                  className="w-4 h-4 rounded border-edge text-accent focus:ring-accent"
                />
                <label htmlFor="requires-contract" className="text-sm text-body select-none cursor-pointer">
                  {t('serviceInterfaces.fieldRequiresContract', 'Requires Contract')}
                </label>
              </div>

              {/* Error from mutation */}
              {mutation.isError && (
                <p className="text-sm text-danger">
                  {t('common.errorSaving', 'Failed to save. Please try again.')}
                </p>
              )}

              {/* Actions */}
              <div className="flex justify-end gap-3 pt-4 border-t border-edge">
                <Button
                  variant="secondary"
                  type="button"
                  onClick={() => navigate(`/services/${serviceId}`)}
                >
                  {t('serviceInterfaces.cancel', 'Cancel')}
                </Button>
                <Button
                  type="button"
                  onClick={handleSubmit}
                  loading={mutation.isPending}
                >
                  <Check size={14} className="mr-1" />
                  {t('serviceInterfaces.submit', 'Create Interface')}
                </Button>
              </div>
            </div>
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}
