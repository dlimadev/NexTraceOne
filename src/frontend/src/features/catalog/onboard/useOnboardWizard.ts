import { useMemo, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { serviceCatalogApi } from '../api';
import { useContractDraftForm } from '../../contracts/create/useContractDraftForm';
import type { ServiceType, InterfaceType, ExposureType } from '../../../types';
import type { OnboardStep, OnboardStepMeta } from './OnboardWizardShell';
import {
  EMPTY_IDENTITY, EMPTY_INTERFACE,
  validateIdentity, validateInterface,
  type ServiceIdentityValues, type ServiceInterfaceValues,
} from './onboardValidation';

const STEP_ORDER: OnboardStep[] = ['identity', 'interface', 'contract', 'review'];

export function useOnboardWizard() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [activeStep, setActiveStep] = useState<OnboardStep>('identity');
  const [identity, setIdentity] = useState<ServiceIdentityValues>(EMPTY_IDENTITY);
  const [iface, setIface] = useState<ServiceInterfaceValues>(EMPTY_INTERFACE);
  const [interfaceTouched, setInterfaceTouched] = useState(false);
  const [serviceId, setServiceId] = useState('');
  const [createdDraftId, setCreatedDraftId] = useState('');
  const [savedInterface, setSavedInterface] = useState<ServiceInterfaceValues | null>(null);
  const [error, setError] = useState<string | null>(null);

  const contractForm = useContractDraftForm({
    onCreated: (draftId) => { setCreatedDraftId(draftId); setActiveStep('review'); },
  });

  const setIdentityField = useCallback(
    <K extends keyof ServiceIdentityValues>(k: K, v: ServiceIdentityValues[K]) =>
      setIdentity((prev) => ({ ...prev, [k]: v })), []);

  const setInterfaceField = useCallback(
    <K extends keyof ServiceInterfaceValues>(k: K, v: ServiceInterfaceValues[K]) => {
      setInterfaceTouched(true);
      setIface((prev) => ({ ...prev, [k]: v }));
    }, []);

  const identityErrors = useMemo(() => validateIdentity(identity), [identity]);
  const interfaceErrors = useMemo(() => validateInterface(iface), [iface]);

  const registerMutation = useMutation({
    mutationFn: () => serviceCatalogApi.registerService({
      name: identity.name, domain: identity.domain, team: identity.teamName,
      description: identity.description || undefined, serviceType: identity.serviceType,
      criticality: identity.criticality, exposureType: identity.exposureType,
      technicalOwner: identity.technicalOwner || undefined,
      businessOwner: identity.businessOwner || undefined,
      documentationUrl: identity.documentationUrl || undefined,
      repositoryUrl: identity.repositoryUrl || undefined,
    }),
    onSuccess: (res) => {
      const id = res?.id ?? '';
      setServiceId(id);
      contractForm.setLinkedServiceId(id);
      contractForm.setSelectedServiceType(identity.serviceType as ServiceType);
      setError(null);
      setActiveStep('interface');
    },
    onError: () => setError(t('onboard.error.create', 'Could not create the service.')),
  });

  const interfaceMutation = useMutation({
    mutationFn: () => serviceCatalogApi.createServiceInterface({
      serviceAssetId: serviceId,
      name: iface.name, interfaceType: iface.interfaceType as InterfaceType,
      description: iface.description || undefined,
      exposureScope: iface.exposureScope as ExposureType,
      basePath: iface.basePath || undefined, topicName: iface.topicName || undefined,
      wsdlNamespace: iface.wsdlNamespace || undefined,
      grpcServiceName: iface.grpcServiceName || undefined,
      scheduleCron: iface.scheduleCron || undefined,
      documentationUrl: iface.documentationUrl || undefined,
      requiresContract: iface.requiresContract,
    }),
    onSuccess: () => { setSavedInterface(iface); setError(null); setActiveStep('contract'); },
    onError: () => setError(t('onboard.error.interface', 'Could not create the interface.')),
  });

  const stepIndex = STEP_ORDER.indexOf(activeStep);
  const isFirstStep = stepIndex === 0;
  const isLastStep = activeStep === 'review';

  const pending =
    registerMutation.isPending || interfaceMutation.isPending || contractForm.createMutation.isPending;

  const canSkip = activeStep === 'interface' || activeStep === 'contract';

  const canGoNext = (() => {
    if (activeStep === 'identity') return Object.keys(identityErrors).length === 0;
    if (activeStep === 'interface') return !interfaceTouched || Object.keys(interfaceErrors).length === 0;
    if (activeStep === 'contract') return contractForm.canCreate || (!contractForm.selectedType && !contractForm.selectedMode);
    return true; // review
  })();

  const onNext = useCallback(() => {
    setError(null);
    if (activeStep === 'identity') { registerMutation.mutate(); return; }
    if (activeStep === 'interface') {
      if (interfaceTouched && iface.name.trim()) { interfaceMutation.mutate(); return; }
      setActiveStep('contract'); return;
    }
    if (activeStep === 'contract') {
      if (contractForm.selectedType && contractForm.selectedMode) { contractForm.createMutation.mutate(); return; }
      setActiveStep('review'); return;
    }
    navigate(`/services/${serviceId}`); // review → finish
  }, [activeStep, interfaceTouched, iface.name, contractForm, registerMutation, interfaceMutation, navigate, serviceId]);

  const onBack = useCallback(() => {
    const prev = STEP_ORDER[Math.max(stepIndex - 1, 0)];
    if (prev) setActiveStep(prev);
  }, [stepIndex]);

  const onSkip = useCallback(() => {
    if (activeStep === 'interface') { setSavedInterface(null); setActiveStep('contract'); }
    else if (activeStep === 'contract') { setActiveStep('review'); }
  }, [activeStep]);

  const onCancel = useCallback(() => {
    // Passo 1 ainda não persistiu → descarta; depois do passo 1 o serviço existe.
    if (serviceId) navigate(`/services/${serviceId}`);
    else navigate('/services');
  }, [serviceId, navigate]);

  const steps: OnboardStepMeta[] = [
    { id: 'identity', label: t('onboard.steps.identity', 'Identity & ownership') },
    { id: 'interface', label: t('onboard.steps.interface', 'Interface'), optional: true },
    { id: 'contract', label: t('onboard.steps.contract', 'Contract'), optional: true },
    { id: 'review', label: t('onboard.steps.review', 'Review') },
  ];

  return {
    activeStep, steps, isFirstStep, isLastStep, canSkip, canGoNext, pending,
    identity, identityErrors, setIdentityField,
    interface: iface, interfaceErrors, setInterfaceField, interfaceTouched,
    savedInterface, contractForm, serviceId, createdDraftId, error,
    onNext, onBack, onSkip, onCancel,
  };
}
