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
import { Button } from '../../../shared/ui';
import { serviceCatalogApi } from '../api';
import { ServiceInterfaceForm } from '../onboard/ServiceInterfaceForm';
import { EMPTY_INTERFACE, type ServiceInterfaceValues } from '../onboard/onboardValidation';
import type { InterfaceType, ExposureType } from '../../../types';

/** Página de criação de interface de serviço. */
export function CreateServiceInterfacePage() {
  const { t } = useTranslation();
  const { serviceId } = useParams<{ serviceId: string }>();
  const navigate = useNavigate();

  const [form, setForm] = useState<ServiceInterfaceValues>(EMPTY_INTERFACE);
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
        interfaceType: form.interfaceType as InterfaceType,
        description: form.description || undefined,
        exposureScope: form.exposureScope as ExposureType,
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

  const set = useCallback(<K extends keyof ServiceInterfaceValues>(
    field: K,
    value: ServiceInterfaceValues[K],
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

              <ServiceInterfaceForm
                values={form}
                errors={errors}
                onChange={(field, value) => set(field, value)}
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
