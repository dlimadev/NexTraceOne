import { useCallback } from 'react';
import { contractsApi } from '../api/contracts';

/**
 * Hook para exportar/download do spec content de uma versão de contrato.
 */
export function useContractExport() {
  const exportVersion = useCallback(async (contractVersionId: string, fileName?: string) => {
    const result = await contractsApi.exportVersion(contractVersionId);
    const blob = new Blob([result.specContent], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName ?? `contract.${result.format}`;
    a.click();
    URL.revokeObjectURL(url);
  }, []);

  return { exportVersion };
}
