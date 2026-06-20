import type React from 'react';
import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  SERVICE_TYPES as CONTRACT_TYPES,
  PROTOCOL_BY_TYPE,
  type ContractTypeValue,
} from '../shared/constants';
import { supportsContracts, allowedContractTypes } from '../shared/serviceContractPolicy';
import { contractStudioApi } from '../api/contractStudio';
import type { ContractType, ContractProtocol } from '../types';
import type { ServiceType } from '../../../types';
import { useAuth } from '../../../contexts/AuthContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';
import type { CreationMode } from './contractCreateConstants';

interface UseContractDraftFormArgs {
  prefilledServiceId?: string;
  initialType?: ContractTypeValue | null;
  initialMode?: CreationMode | null;
}

/**
 * Hook que concentra todo o estado de formulário, queries, valores derivados e
 * a mutação de criação de draft do fluxo "Registrar Contrato".
 * A lógica de criação é preservada verbatim da CreateContractPage original.
 */
export function useContractDraftForm(args: UseContractDraftFormArgs) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user } = useAuth();
  const currentActor = user?.email || user?.fullName || user?.id || 'system';

  const prefilledServiceId = args.prefilledServiceId ?? '';
  const hasPrefilledService = prefilledServiceId.length > 0;

  const [selectedType, setSelectedType] = useState<ContractTypeValue | null>(args.initialType ?? null);
  const [selectedMode, setSelectedMode] = useState<CreationMode | null>(args.initialMode ?? null);
  const [selectedProtocol, setSelectedProtocol] = useState<ContractProtocol | ''>('');
  const [linkedServiceId, setLinkedServiceId] = useState(prefilledServiceId);
  const [selectedServiceType, setSelectedServiceType] = useState<ServiceType | null>(null);
  const [serviceSearch, setServiceSearch] = useState('');

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [importContent, setImportContent] = useState('');
  const [aiPrompt, setAiPrompt] = useState('');

  // SOAP-specific fields
  const [soapServiceName, setSoapServiceName] = useState('');
  const [soapTargetNamespace, setSoapTargetNamespace] = useState('http://example.com/service');
  const [soapVersion, setSoapVersion] = useState<'1.1' | '1.2'>('1.1');
  const [soapEndpointUrl, setSoapEndpointUrl] = useState('');

  // Event/AsyncAPI-specific fields
  const [asyncApiVersion, setAsyncApiVersion] = useState('2.6.0');
  const [defaultContentType, setDefaultContentType] = useState('application/json');

  // Background Service-specific fields
  const [bgServiceName, setBgServiceName] = useState('');
  const [bgCategory, setBgCategory] = useState('Job');
  const [bgTriggerType, setBgTriggerType] = useState('OnDemand');
  const [bgScheduleExpression, setBgScheduleExpression] = useState('');

  const isSoapType = selectedType === 'Soap';
  const isEventType = selectedType === 'Event';
  const isBackgroundServiceType = selectedType === 'BackgroundService';

  // ── Queries ─────────────────────────────────────────────────────────────────

  const servicesQuery = useQuery({
    queryKey: ['catalog-services-for-contracts'],
    queryFn: () => serviceCatalogApi.listServices(),
  });

  const prefilledServiceQuery = useQuery({
    queryKey: ['catalog-service-for-contract-create', prefilledServiceId],
    queryFn: () => serviceCatalogApi.listServices(),
    enabled: hasPrefilledService,
    select: (data) => data?.items?.find((s) => s.serviceId === prefilledServiceId) ?? null,
  });

  const effectiveServiceType: ServiceType | null =
    selectedServiceType ?? (prefilledServiceQuery.data?.serviceType ?? null);

  const filteredContractTypes = effectiveServiceType
    ? CONTRACT_TYPES.filter((ct) =>
        allowedContractTypes(effectiveServiceType).includes(ct.value as ContractTypeValue),
      )
    : CONTRACT_TYPES;

  const serviceSupportsContracts = effectiveServiceType ? supportsContracts(effectiveServiceType) : true;

  const availableServices = servicesQuery.data?.items ?? [];

  const prefilledService = prefilledServiceQuery.data ?? null;
  const selectedServiceDisplay =
    prefilledService ??
    (linkedServiceId ? availableServices.find((s) => s.serviceId === linkedServiceId) ?? null : null);

  const filteredServices = useMemo(() => {
    const q = serviceSearch.toLowerCase();
    if (!q) return availableServices;
    return availableServices.filter(
      (svc) =>
        svc.displayName.toLowerCase().includes(q) ||
        svc.domain.toLowerCase().includes(q) ||
        svc.teamName.toLowerCase().includes(q),
    );
  }, [availableServices, serviceSearch]);

  // ── Mutation ─────────────────────────────────────────────────────────────────

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!selectedType || !selectedProtocol || !linkedServiceId) {
        throw new Error(t('contracts.create.missingRequiredFields', 'Missing required fields'));
      }

      if (selectedMode === 'ai') {
        const aiDraft = await contractStudioApi.generateFromAi({
          title,
          author: currentActor,
          contractType: selectedType as ContractType,
          protocol: selectedProtocol as ContractProtocol,
          prompt: aiPrompt,
          serviceId: linkedServiceId,
        });
        return { draftId: aiDraft.draftId };
      }

      if (isSoapType) {
        const soapDraft = await contractStudioApi.createSoapDraft({
          title,
          author: currentActor,
          serviceName: soapServiceName || title,
          targetNamespace: soapTargetNamespace || 'http://example.com/service',
          soapVersion,
          serviceId: linkedServiceId,
          description,
          endpointUrl: soapEndpointUrl || undefined,
        });
        if (selectedMode === 'import' && importContent.trim()) {
          await contractStudioApi.updateContent(soapDraft.draftId, {
            specContent: importContent,
            format: 'xml',
            editedBy: currentActor,
          });
        }
        return { draftId: soapDraft.draftId };
      }

      if (isEventType) {
        const eventDraft = await contractStudioApi.createEventDraft({
          title,
          author: currentActor,
          asyncApiVersion,
          serviceId: linkedServiceId,
          description,
          defaultContentType,
        });
        if (selectedMode === 'import' && importContent.trim()) {
          await contractStudioApi.updateContent(eventDraft.draftId, {
            specContent: importContent,
            format: 'json',
            editedBy: currentActor,
          });
        }
        return { draftId: eventDraft.draftId };
      }

      if (isBackgroundServiceType) {
        const bgDraft = await contractStudioApi.createBackgroundServiceDraft({
          title,
          author: currentActor,
          serviceName: bgServiceName || title,
          category: bgCategory,
          triggerType: bgTriggerType,
          serviceId: linkedServiceId,
          description,
          scheduleExpression: bgScheduleExpression || undefined,
        });
        return { draftId: bgDraft.draftId };
      }

      const createdDraft = await contractStudioApi.createDraft({
        title,
        author: currentActor,
        contractType: selectedType as ContractType,
        protocol: selectedProtocol as ContractProtocol,
        serviceId: linkedServiceId,
        description,
      });
      if (selectedMode === 'import' && importContent.trim()) {
        await contractStudioApi.updateContent(createdDraft.draftId, {
          specContent: importContent,
          format: 'yaml',
          editedBy: currentActor,
        });
      }
      return createdDraft;
    },
    onSuccess: (data) => {
      navigate(`/contracts/studio/${data.draftId}`);
    },
  });

  // ── Derived state ─────────────────────────────────────────────────────────

  const protocols = selectedType ? PROTOCOL_BY_TYPE[selectedType] : [];

  const canProceedFromService = linkedServiceId.length > 0 && serviceSupportsContracts;
  const canProceedFromConfigure = !!selectedType && !!selectedMode;
  const canCreate =
    !!title &&
    !!selectedProtocol &&
    !!linkedServiceId &&
    (() => {
      if (selectedMode === 'ai') return !!aiPrompt.trim();
      if (selectedMode === 'import') return !!importContent.trim();
      return true;
    })();

  // ── setField — setter curry genérico para campos de texto/select ────────────

  const fieldSetters: Record<string, (value: string) => void> = {
    title: setTitle,
    description: setDescription,
    importContent: setImportContent,
    aiPrompt: setAiPrompt,
    soapServiceName: setSoapServiceName,
    soapTargetNamespace: setSoapTargetNamespace,
    soapEndpointUrl: setSoapEndpointUrl,
    asyncApiVersion: setAsyncApiVersion,
    defaultContentType: setDefaultContentType,
    bgServiceName: setBgServiceName,
    bgCategory: setBgCategory,
    bgTriggerType: setBgTriggerType,
    bgScheduleExpression: setBgScheduleExpression,
    serviceSearch: setServiceSearch,
  };

  type FieldKey = keyof typeof fieldSetters;

  const setField =
    (key: FieldKey) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) =>
      fieldSetters[key]?.(e.target.value);

  // ── Live summary (cartão de identidade) ─────────────────────────────────────

  const summary = useMemo(
    () => ({
      title,
      serviceName: selectedServiceDisplay?.displayName ?? '',
      type: selectedType,
      protocol: selectedProtocol,
      mode: selectedMode,
      proposedVersion: '1.0.0',
      author: currentActor,
    }),
    [title, selectedServiceDisplay, selectedType, selectedProtocol, selectedMode, currentActor],
  );

  return {
    // setter genérico
    setField,
    // identidade / autor
    currentActor,
    // type + mode + protocol
    selectedType,
    setSelectedType,
    selectedMode,
    setSelectedMode,
    selectedProtocol,
    setSelectedProtocol,
    // serviço
    linkedServiceId,
    setLinkedServiceId,
    selectedServiceType,
    setSelectedServiceType,
    serviceSearch,
    setServiceSearch,
    effectiveServiceType,
    filteredContractTypes,
    availableServices,
    filteredServices,
    selectedServiceDisplay,
    serviceSupportsContracts,
    // campos comuns
    title,
    setTitle,
    description,
    setDescription,
    importContent,
    setImportContent,
    aiPrompt,
    setAiPrompt,
    // SOAP
    soapServiceName,
    setSoapServiceName,
    soapTargetNamespace,
    setSoapTargetNamespace,
    soapVersion,
    setSoapVersion,
    soapEndpointUrl,
    setSoapEndpointUrl,
    // Event/AsyncAPI
    asyncApiVersion,
    setAsyncApiVersion,
    defaultContentType,
    setDefaultContentType,
    // Background Service
    bgServiceName,
    setBgServiceName,
    bgCategory,
    setBgCategory,
    bgTriggerType,
    setBgTriggerType,
    bgScheduleExpression,
    setBgScheduleExpression,
    // type flags
    isSoapType,
    isEventType,
    isBackgroundServiceType,
    // derivados
    protocols,
    canProceedFromService,
    canProceedFromConfigure,
    canCreate,
    summary,
    // queries + mutation
    servicesQuery,
    prefilledServiceQuery,
    createMutation,
  };
}
