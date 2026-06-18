import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { FileText, Search, CheckCircle2, BookOpen, Link, Server, GitBranch } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { SearchInput } from '../../../components/SearchInput';
import { FilterChip } from '../../../components/FilterChip';
import client from '../../../api/client';

type SectionKey = 'Overview' | 'Ownership' | 'Contracts' | 'Dependencies' | 'SLOs' | 'Runbooks' | 'RecentChanges';

interface AutoDocumentationResponse {
  serviceName: string;
  generatedAt: string;
  sections: AutoDocSection[];
  runbookCount: number;
  documentCount: number;
  lastUpdated: string | null;
}

interface AutoDocSection {
  sectionKey: SectionKey;
  title: string;
  content: string;
  available: boolean;
}

const SECTION_ICONS: Record<SectionKey, React.ReactNode> = {
  Overview: <FileText size={16} />,
  Ownership: <Server size={16} />,
  Contracts: <Link size={16} />,
  Dependencies: <GitBranch size={16} />,
  SLOs: <CheckCircle2 size={16} />,
  Runbooks: <BookOpen size={16} />,
  RecentChanges: <Search size={16} />,
};

const ALL_SECTIONS: SectionKey[] = ['Overview', 'Ownership', 'Contracts', 'Dependencies', 'SLOs', 'Runbooks', 'RecentChanges'];

const useAutoDocumentation = (serviceName: string, sections: SectionKey[]) =>
  useQuery({
    queryKey: ['auto-documentation', serviceName, sections],
    queryFn: () =>
      client
        .get<AutoDocumentationResponse>(`/knowledge/auto-documentation/${serviceName}`, {
          params: { sections: sections.join(',') },
        })
        .then((r) => r.data),
    enabled: serviceName.length > 0,
  });

export function AutoDocumentationPage() {
  const { t } = useTranslation();
  const { serviceName: paramServiceName } = useParams<{ serviceName?: string }>();
  const [serviceName, setServiceName] = useState(paramServiceName ?? '');
  const [inputValue, setInputValue] = useState(paramServiceName ?? '');
  const [selectedSections, setSelectedSections] = useState<SectionKey[]>(ALL_SECTIONS);

  const { data, isLoading, isError, refetch } = useAutoDocumentation(serviceName, selectedSections);

  const toggleSection = (key: SectionKey) => {
    setSelectedSections((prev) =>
      prev.includes(key) ? prev.filter((s) => s !== key) : [...prev, key]
    );
  };

  const handleSearch = () => {
    setServiceName(inputValue.trim());
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('knowledge.autoDoc.title')}
        subtitle={t('knowledge.autoDoc.subtitle')}
        icon={<FileText size={24} />}
      />

      {/* Search controls */}
      <Card className="mb-6">
        <CardBody className="p-4">
          {/* Campo de pesquisa DS — substitui raw <input type="text"> */}
          <div className="flex flex-col sm:flex-row gap-3">
            <SearchInput
              size="sm"
              className="flex-1"
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              placeholder={t('knowledge.autoDoc.serviceNamePlaceholder')}
              aria-label={t('knowledge.autoDoc.serviceNamePlaceholder')}
            />
            <Button
              variant="primary"
              icon={<Search size={14} />}
              onClick={handleSearch}
              disabled={!inputValue.trim()}
            >
              {t('knowledge.autoDoc.generate')}
            </Button>
          </div>
          {/* Chips de filtro DS — substituem raw <button> com border-indigo-* hardcoded */}
          <div className="mt-3 flex flex-wrap gap-2">
            {ALL_SECTIONS.map((key) => (
              <FilterChip
                key={key}
                label={t(`knowledge.autoDoc.sections.${key}`)}
                icon={SECTION_ICONS[key]}
                active={selectedSections.includes(key)}
                onClick={() => toggleSection(key)}
              />
            ))}
          </div>
        </CardBody>
      </Card>

      {isLoading && <PageLoadingState message={t('knowledge.autoDoc.generating')} />}
      {isError && <PageErrorState message={t('knowledge.autoDoc.error')} onRetry={() => refetch()} />}

      {data && (
        <>
          <div className="mb-4 flex items-center justify-between">
            <div>
              <h2 className="text-lg font-semibold text-heading">{data.serviceName}</h2>
              <p className="text-sm text-muted">
                {t('knowledge.autoDoc.generatedAt', { date: new Date(data.generatedAt).toLocaleString() })}
              </p>
            </div>
            <div className="flex gap-3">
              <Badge variant="secondary">{t('knowledge.autoDoc.docs', { count: data.documentCount })}</Badge>
              <Badge variant="secondary">{t('knowledge.autoDoc.runbooks', { count: data.runbookCount })}</Badge>
            </div>
          </div>

          {data.sections.map((section) => (
            <PageSection key={section.sectionKey} title={section.title}>
              {section.available ? (
                <Card>
                  <CardBody className="p-4">
                    <div className="flex items-start gap-3">
                      {/* text-indigo-500 → token semântico text-accent */}
                      <span className="mt-1 text-accent">{SECTION_ICONS[section.sectionKey as SectionKey]}</span>
                      <p className="text-sm text-body whitespace-pre-line">
                        {section.content}
                      </p>
                    </div>
                  </CardBody>
                </Card>
              ) : (
                <div className="rounded border border-dashed border-edge py-6 text-center text-sm text-muted">
                  {t('knowledge.autoDoc.sectionUnavailable')}
                </div>
              )}
            </PageSection>
          ))}
        </>
      )}

      {!serviceName && !isLoading && (
        <div className="text-center py-12 text-muted">
          <FileText size={40} className="mx-auto mb-3 opacity-40" />
          <p>{t('knowledge.autoDoc.enterServiceName')}</p>
        </div>
      )}
    </PageContainer>
  );
}
