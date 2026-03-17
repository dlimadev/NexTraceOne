import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { BookOpen, Plus, Search, Tag, Link } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import type { GlossaryTerm } from '../../types/domain';

interface GlossarySectionProps {
  contractId?: string;
  className?: string;
}

/** Mock glossary terms para demonstração da secção. */
const MOCK_TERMS: GlossaryTerm[] = [
  {
    id: 'gt-1',
    term: 'API Asset',
    definition: 'A uniquely identified API or service contract registered in the platform, tracked across versions.',
    aliases: ['Service Contract', 'API Contract'],
    relatedTerms: ['Contract Version', 'Specification'],
  },
  {
    id: 'gt-2',
    term: 'Specification',
    definition: 'The formal definition of a contract, written in a standard format such as OpenAPI, AsyncAPI, or WSDL.',
    aliases: ['Spec', 'Contract Spec'],
    relatedTerms: ['API Asset', 'Schema'],
  },
  {
    id: 'gt-3',
    term: 'Lifecycle State',
    definition: 'The current governance stage of a contract version: Draft, InReview, Approved, Locked, Deprecated, Sunset, or Retired.',
    relatedTerms: ['Approval', 'Governance'],
  },
  {
    id: 'gt-4',
    term: 'Breaking Change',
    definition: 'A modification to a contract that is incompatible with the previous version and may disrupt consumers.',
    aliases: ['Incompatible Change'],
    relatedTerms: ['Semantic Diff', 'Versioning'],
  },
  {
    id: 'gt-5',
    term: 'Consumer',
    definition: 'A service, application, or external system that depends on and uses this contract.',
    relatedTerms: ['Producer', 'Dependency'],
  },
];

/**
 * Secção de Glossário do studio — termos de domínio associados ao contrato.
 * Permite pesquisar, visualizar definições, aliases e termos relacionados.
 */
export function GlossarySection({ className = '' }: GlossarySectionProps) {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const filtered = MOCK_TERMS.filter((term) => {
    if (!search.trim()) return true;
    const q = search.toLowerCase();
    return (
      term.term.toLowerCase().includes(q) ||
      term.definition.toLowerCase().includes(q) ||
      term.aliases?.some((a) => a.toLowerCase().includes(q))
    );
  });

  return (
    <div className={`space-y-4 ${className}`}>
      <div className="flex items-center justify-between flex-wrap gap-2">
        <div className="flex items-center gap-2">
          <BookOpen size={14} className="text-accent" />
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.studio.glossary.title', 'Domain Glossary')}
          </h3>
          <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
            {MOCK_TERMS.length}
          </span>
        </div>

        <div className="flex items-center gap-2">
          <div className="relative">
            <Search size={12} className="absolute left-2 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('contracts.studio.glossary.searchPlaceholder', 'Search terms...')}
              className="text-xs bg-elevated border border-edge rounded pl-7 pr-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent w-48"
            />
          </div>
          <button className="inline-flex items-center gap-1 px-2.5 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
            <Plus size={10} /> {t('contracts.studio.glossary.addTerm', 'Add Term')}
          </button>
        </div>
      </div>

      {filtered.length === 0 ? (
        <EmptyState
          title={t('contracts.studio.glossary.emptyTitle', 'No terms found')}
          description={t('contracts.studio.glossary.emptyDescription', 'Add domain terms to help teams understand the contract vocabulary.')}
          icon={<BookOpen size={24} />}
        />
      ) : (
        <div className="space-y-2">
          {filtered.map((term) => (
            <Card key={term.id}>
              <button
                onClick={() => setExpandedId(expandedId === term.id ? null : term.id)}
                className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors"
              >
                <BookOpen size={12} className="text-accent flex-shrink-0" />
                <span className="text-xs font-semibold text-heading flex-1">{term.term}</span>
                {term.aliases && term.aliases.length > 0 && (
                  <span className="text-[10px] text-muted">{term.aliases.length} aliases</span>
                )}
              </button>

              {expandedId === term.id && (
                <CardBody className="pt-0 px-4 pb-4 border-t border-edge">
                  <p className="text-xs text-body leading-relaxed mb-3">{term.definition}</p>

                  {term.aliases && term.aliases.length > 0 && (
                    <div className="flex items-center gap-1.5 mb-2">
                      <Tag size={10} className="text-muted" />
                      <span className="text-[10px] text-muted mr-1">Aliases:</span>
                      {term.aliases.map((alias) => (
                        <span key={alias} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated border border-edge text-body">
                          {alias}
                        </span>
                      ))}
                    </div>
                  )}

                  {term.relatedTerms && term.relatedTerms.length > 0 && (
                    <div className="flex items-center gap-1.5">
                      <Link size={10} className="text-muted" />
                      <span className="text-[10px] text-muted mr-1">Related:</span>
                      {term.relatedTerms.map((rt) => (
                        <span key={rt} className="text-[10px] px-1.5 py-0.5 rounded bg-accent/10 border border-accent/20 text-accent">
                          {rt}
                        </span>
                      ))}
                    </div>
                  )}
                </CardBody>
              )}
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
