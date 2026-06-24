/**
 * Página de criação de uma nova interface de exposição de serviço.
 * Pertence ao módulo Service Catalog — bounded context de Service Interfaces.
 */
import { useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Layers, Check } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button, TextField, TextArea, Select, Checkbox } from '../../../shared/ui';
import { serviceCatalogApi } from '../api';
import type { InterfaceType, ExposureType } from '../../../types';

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

  const interfaceTypeOptions = [
    { value: 'RestApi', label: t('serviceInterfaces.typeRestApi', 'REST API') },
    { value: 'GraphqlApi', label: t('serviceInterfaces.typeGraphqlApi', 'GraphQL API') },
    { value: 'GrpcService', label: t('serviceInterfaces.typeGrpcService', 'gRPC Service') },
    { value: 'SoapService', label: t('serviceInterfaces.typeSoapService', 'SOAP Service') },
    { value: 'ZosConnectApi', label: t('serviceInterfaces.typeZosConnectApi', 'z/OS Connect API') },
    { value: 'KafkaProducer', label: t('serviceInterfaces.typeKafkaProducer', 'Kafka Producer') },
    { value: 'KafkaConsumer', label: t('serviceInterfaces.typeKafkaConsumer', 'Kafka Consumer') },
    { value: 'WebhookProducer', label: t('serviceInterfaces.typeWebhookProducer', 'Webhook Producer') },
    { value: 'WebhookConsumer', label: t('serviceInterfaces.typeWebhookConsumer', 'Webhook Consumer') },
    { value: 'MqQueue', label: t('serviceInterfaces.typeMqQueue', 'MQ Queue') },
    { value: 'BackgroundWorker', label: t('serviceInterfaces.typeBackgroundWorker', 'Background Worker') },
    { value: 'ScheduledJob', label: t('serviceInterfaces.typeScheduledJob', 'Scheduled Job') },
    { value: 'IntegrationBridge', label: t('serviceInterfaces.typeIntegrationBridge', 'Integration Bridge') },
  ];

  const exposureScopeOptions = [
    { value: 'Internal', label: t('catalog.badges.exposure.Internal', 'Internal') },
    { value: 'Partner', label: t('catalog.badges.exposure.Partner', 'Partner') },
    { value: 'External', label: t('catalog.badges.exposure.External', 'External') },
  ];

  return (
    <PageContainer className="animate-fade-in">
      {/* ── Breadcrumb / Back ── */}
      <div className="mb-4">
        <Button
          variant="ghost"
          size="sm"
          icon={<ArrowLeft size={14} />}
          onClick={() => navigate(`/services/${serviceId}`)}
        >
          {service.displayName || service.name}
        </Button>
      </div>

      <PageHeader
        title={t('serviceInterfaces.createTitle', 'New Interface')}
        subtitle={`${t('serviceInterfaces.createSubtitle', 'Register a new interface for this service')} — ${service.displayName || service.name}`}
        icon={<Layers size={24} />}
      />

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
                <TextField
                  label={t('serviceInterfaces.fieldName', 'Interface Name')}
                  value={form.name}
                  onChange={(e) => set('name', e.target.value)}
                  placeholder={t('serviceInterfaces.fieldNamePlaceholder', 'Orders REST API v1')}
                  required
                  autoFocus
                  error={errors.name}
                />
                <Select
                  label={t('serviceInterfaces.fieldType', 'Interface Type')}
                  value={form.interfaceType}
                  onChange={(e) => set('interfaceType', e.target.value as InterfaceType)}
                  options={interfaceTypeOptions}
                />
              </div>

              {/* Description */}
              <TextArea
                label={t('serviceInterfaces.fieldDescription', 'Description')}
                value={form.description}
                onChange={(e) => set('description', e.target.value)}
                rows={3}
              />

              {/* Exposure */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Select
                  label={t('serviceInterfaces.fieldExposure', 'Exposure Scope')}
                  value={form.exposureScope}
                  onChange={(e) => set('exposureScope', e.target.value as ExposureType)}
                  options={exposureScopeOptions}
                />
              </div>

              {/* Conditional fields */}
              {showBasePath && (
                <TextField
                  label={t('serviceInterfaces.fieldBasePath', 'Base Path')}
                  value={form.basePath}
                  onChange={(e) => set('basePath', e.target.value)}
                  placeholder={t('serviceInterfaces.fieldBasePathPlaceholder', '/api/orders')}
                  className="font-mono"
                />
              )}

              {showWsdl && (
                <TextField
                  label={t('serviceInterfaces.fieldWsdlNamespace', 'WSDL Namespace')}
                  value={form.wsdlNamespace}
                  onChange={(e) => set('wsdlNamespace', e.target.value)}
                  className="font-mono"
                />
              )}

              {showTopic && (
                <TextField
                  label={t('serviceInterfaces.fieldTopicName', 'Topic Name')}
                  value={form.topicName}
                  onChange={(e) => set('topicName', e.target.value)}
                  placeholder={t('serviceInterfaces.fieldTopicNamePlaceholder', 'orders.events.v1')}
                  className="font-mono"
                />
              )}

              {showGrpc && (
                <TextField
                  label={t('serviceInterfaces.fieldGrpcServiceName', 'Proto Service Name')}
                  value={form.grpcServiceName}
                  onChange={(e) => set('grpcServiceName', e.target.value)}
                  className="font-mono"
                />
              )}

              {showCron && (
                <TextField
                  label={t('serviceInterfaces.fieldScheduleCron', 'Cron Expression')}
                  value={form.scheduleCron}
                  onChange={(e) => set('scheduleCron', e.target.value)}
                  placeholder="0 */5 * * *"
                  className="font-mono"
                />
              )}

              {/* Documentation URL */}
              <TextField
                label={t('serviceInterfaces.fieldDocumentationUrl', 'Documentation URL')}
                type="url"
                value={form.documentationUrl}
                onChange={(e) => set('documentationUrl', e.target.value)}
                className="font-mono"
              />

              {/* Requires Contract */}
              <Checkbox
                id="requires-contract"
                checked={form.requiresContract}
                onChange={(e) => set('requiresContract', e.target.checked)}
                label={t('serviceInterfaces.fieldRequiresContract', 'Requires Contract')}
              />

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
                  variant="primary"
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
