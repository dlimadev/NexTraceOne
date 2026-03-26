import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractStudioApi } from '../api/contractStudio';

const soapKeys = {
  all: ['soap-contracts'] as const,
  detail: (contractVersionId: string) => [...soapKeys.all, 'detail', contractVersionId] as const,
  draftAll: ['soap-drafts'] as const,
};

/**
 * Hook para importar um contrato WSDL com extração real de metadados SOAP.
 * Chama POST /api/v1/contracts/wsdl/import e retorna ContractVersionId + SoapContractDetail.
 */
export function useWsdlImport() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      apiAssetId: string;
      semVer: string;
      wsdlContent: string;
      importedFrom: string;
      endpointUrl?: string;
      wsdlSourceUrl?: string;
      soapVersion?: '1.1' | '1.2';
    }) => contractsApi.importWsdl(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      queryClient.invalidateQueries({ queryKey: soapKeys.all });
    },
  });
}

/**
 * Hook para criar um draft SOAP/WSDL com metadados específicos.
 * Chama POST /api/v1/contracts/drafts/soap.
 */
export function useCreateSoapDraft() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      title: string;
      author: string;
      serviceName: string;
      targetNamespace: string;
      soapVersion?: '1.1' | '1.2';
      serviceId?: string;
      description?: string;
      endpointUrl?: string;
      portTypeName?: string;
      bindingName?: string;
      operationsJson?: string;
    }) => contractStudioApi.createSoapDraft(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contract-drafts'] });
      queryClient.invalidateQueries({ queryKey: soapKeys.draftAll });
    },
  });
}

/**
 * Hook para consultar os detalhes SOAP/WSDL de uma versão de contrato publicada.
 * Chama GET /api/v1/contracts/{contractVersionId}/soap-detail.
 */
export function useSoapContractDetail(contractVersionId: string | undefined) {
  return useQuery({
    queryKey: soapKeys.detail(contractVersionId ?? ''),
    queryFn: () => contractsApi.getSoapContractDetail(contractVersionId!),
    enabled: Boolean(contractVersionId),
    staleTime: 5 * 60 * 1000,
  });
}

export { soapKeys };
