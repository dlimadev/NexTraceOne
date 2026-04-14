import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { FileCode, Upload, Wand2, Copy, Save, Info } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

interface ContractGenerationResult {
  openApiYaml: string;
  paths: number;
  schemas: number;
  examples: number;
  language: string;
}

const useGenerateContract = (code: string, language: string, enabled: boolean) =>
  useQuery({
    queryKey: ['ai-contract-generator', code.substring(0, 50), language],
    queryFn: () =>
      client
        .post<ContractGenerationResult>('/ai/contract-generator', { code, language })
        .then((r) => r.data),
    enabled,
  });

export function AiContractGeneratorPage() {
  const { t } = useTranslation();
  const [code, setCode] = useState('');
  const [language, setLanguage] = useState('csharp');
  const [generate, setGenerate] = useState(false);
  const { data, isLoading, isError, refetch } = useGenerateContract(code, language, generate && code.length > 10);

  const handleGenerate = () => {
    if (code.length < 10) return;
    setGenerate(true);
  };

  if (isLoading && generate) return <PageLoadingState message={t('aiContractGen.generating')} />;
  if (isError && generate) return <PageErrorState message={t('aiContractGen.error')} onRetry={() => refetch()} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('aiContractGen.title')}
        subtitle={t('aiContractGen.subtitle')}
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-4">
        <Card>
          <CardBody className="p-4">
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                {t('aiContractGen.pasteCode')}
              </label>
              <div className="flex items-center gap-2">
                <select
                  value={language}
                  onChange={(e) => { setLanguage(e.target.value); setGenerate(false); }}
                  className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1"
                >
                  <option value="csharp">C#</option>
                  <option value="java">Java</option>
                  <option value="typescript">TypeScript</option>
                  <option value="python">Python</option>
                </select>
                <Button size="sm" variant="ghost">
                  <Upload size={12} className="mr-1" />
                  {t('aiContractGen.uploadFile')}
                </Button>
              </div>
            </div>
            <textarea
              value={code}
              onChange={(e) => { setCode(e.target.value); setGenerate(false); }}
              placeholder={t('aiContractGen.examplePlaceholder')}
              rows={12}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 text-xs font-mono p-2 resize-y"
            />
            <div className="mt-2 flex items-center gap-2">
              <Info size={12} className="text-gray-400" />
              <span className="text-xs text-gray-400">{t('aiContractGen.supportedFormats')}</span>
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardBody className="p-4">
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                {t('aiContractGen.generatedContract')}
              </label>
              {data && (
                <div className="flex gap-2">
                  <Badge variant="success">{data.language}</Badge>
                  <Button size="sm" variant="ghost">
                    <Copy size={12} className="mr-1" />
                    {t('aiContractGen.copyToClipboard')}
                  </Button>
                  <Button size="sm" variant="ghost">
                    <Save size={12} className="mr-1" />
                    {t('aiContractGen.saveAsContract')}
                  </Button>
                </div>
              )}
            </div>
            <pre className="w-full rounded border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 text-xs font-mono p-2 overflow-auto h-48 text-gray-700 dark:text-gray-300">
              {data?.openApiYaml ?? t('aiContractGen.noOutput')}
            </pre>
            <p className="text-xs text-amber-600 dark:text-amber-400 mt-2">
              {t('aiContractGen.disclaimer')}
            </p>
          </CardBody>
        </Card>
      </div>

      <div className="flex justify-center">
        <Button onClick={handleGenerate} disabled={code.length < 10}>
          <Wand2 size={16} className="mr-2" />
          {t('aiContractGen.generate')}
        </Button>
      </div>
    </PageContainer>
  );
}
