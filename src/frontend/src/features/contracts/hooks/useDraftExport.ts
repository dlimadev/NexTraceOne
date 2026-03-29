import { useCallback, useState } from 'react';
import { contractStudioApi } from '../api/contractStudio';

/**
 * Hook para exportar/download do spec content de um draft de contrato.
 * Permite ao utilizador obter o YAML/JSON/XML antes da publicação
 * para uso com ferramentas externas (dotnet-openapi, NSwag, Kiota).
 */
export function useDraftExport() {
  const [isExporting, setIsExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);

  const exportDraft = useCallback(async (draftId: string, fileName?: string) => {
    setIsExporting(true);
    setExportError(null);
    try {
      const result = await contractStudioApi.exportDraft(draftId);
      const blob = new Blob([result.specContent], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName ?? `${result.title}.${result.format}`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setExportError('Failed to export draft.');
    } finally {
      setIsExporting(false);
    }
  }, []);

  return { exportDraft, isExporting, exportError };
}
