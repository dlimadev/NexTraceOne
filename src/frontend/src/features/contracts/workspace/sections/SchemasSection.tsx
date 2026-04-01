import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, Database, ChevronDown, ChevronRight, Share2, Tag } from 'lucide-react';
import { Card, CardBody } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';

// ── Local types ───────────────────────────────────────────────────────────────

interface SchemaItem {
  name: string;
  type: string;
  description: string;
  properties: SchemaProperty[];
  isShared: boolean;
  usageCount: number;
}

interface SchemaProperty {
  name: string;
  type: string;
  required: boolean;
  description: string;
  format?: string;
  ref?: string;
}

interface SchemasSectionProps {
  specContent: string;
  protocol: string;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Secção de Schemas / Models do workspace.
 * Mostra schemas extraídos do spec content com exploração visual,
 * indicação de schemas partilhados e referências cruzadas.
 */
export function SchemasSection({ specContent, protocol, className = '' }: SchemasSectionProps) {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [expandedName, setExpandedName] = useState<string | null>(null);
  const [filter, setFilter] = useState<'all' | 'shared'>('all');

  const schemas = useMemo(() => parseSchemas(specContent, protocol), [specContent, protocol]);

  const filtered = useMemo(() => {
    let result = schemas;
    if (filter === 'shared') {
      result = result.filter((s) => s.isShared);
    }
    if (search.trim()) {
      const q = search.toLowerCase();
      result = result.filter(
        (s) =>
          s.name.toLowerCase().includes(q) ||
          s.description.toLowerCase().includes(q) ||
          s.properties.some((p) => p.name.toLowerCase().includes(q)),
      );
    }
    return result;
  }, [schemas, search, filter]);

  if (schemas.length === 0) {
    return (
      <div className={className}>
        <EmptyState
          title={t('contracts.schemas.emptyTitle', 'No schemas found')}
          description={t('contracts.schemas.emptyDescription', 'Schemas and models will appear here once the contract specification is defined.')}
          icon={<Database size={24} />}
        />
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-2">
        <div className="flex items-center gap-3">
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.schemas.title', 'Schemas & Models')}
          </h3>
          <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
            {schemas.length}
          </span>
        </div>

        <div className="flex items-center gap-2">
          {/* Filter tabs */}
          <div className="flex items-center bg-panel rounded border border-edge overflow-hidden">
            {(['all', 'shared'] as const).map((f) => (
              <button
                key={f}
                onClick={() => setFilter(f)}
                className={`px-2.5 py-1 text-[10px] font-medium transition-colors ${
                  filter === f ? 'bg-accent text-white' : 'text-muted hover:text-heading'
                }`}
              >
                {f === 'all' ? t('common.all', 'All') : t('contracts.schemas.shared', 'Shared')}
              </button>
            ))}
          </div>

          {/* Search */}
          <div className="relative">
            <Search size={12} className="absolute left-2 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('contracts.schemas.searchPlaceholder', 'Search schemas...')}
              className="text-xs bg-elevated border border-edge rounded pl-7 pr-2 py-1.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent w-48"
            />
          </div>
        </div>
      </div>

      {/* Schema list */}
      <div className="space-y-2">
        {filtered.map((schema) => (
          <SchemaCard
            key={schema.name}
            schema={schema}
            isExpanded={expandedName === schema.name}
            onToggle={() => setExpandedName(expandedName === schema.name ? null : schema.name)}
          />
        ))}
      </div>

      {filtered.length === 0 && search.trim() && (
        <p className="text-xs text-muted text-center py-4">
          {t('contracts.schemas.noResults', 'No schemas match your search.')}
        </p>
      )}
    </div>
  );
}

// ── Schema Card ───────────────────────────────────────────────────────────────

function SchemaCard({
  schema,
  isExpanded,
  onToggle,
}: {
  schema: SchemaItem;
  isExpanded: boolean;
  onToggle: () => void;
}) {
  const { t } = useTranslation();

  return (
    <Card>
      <button onClick={onToggle} className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors">
        {isExpanded ? <ChevronDown size={12} className="text-muted flex-shrink-0" /> : <ChevronRight size={12} className="text-muted flex-shrink-0" />}

        <Database size={14} className="text-accent flex-shrink-0" />
        <span className="text-xs font-semibold text-heading flex-1">{schema.name}</span>

        {schema.isShared && (
          <span className="inline-flex items-center gap-1 text-[10px] text-success px-1.5 py-0.5 rounded bg-success/15 border border-success/25">
            <Share2 size={9} /> {t('contracts.schemas.shared', 'Shared')}
          </span>
        )}

        <span className="text-[10px] text-muted">{schema.properties.length} {t('contracts.schemas.props', 'props')}</span>

        {schema.usageCount > 0 && (
          <span className="text-[10px] text-muted px-1.5 py-0.5 rounded bg-elevated border border-edge">
            {schema.usageCount} {t('contracts.schemas.refs', 'refs')}
          </span>
        )}
      </button>

      {isExpanded && (
        <CardBody className="pt-0 px-4 pb-4 border-t border-edge">
          {schema.description && (
            <p className="text-xs text-muted mb-3">{schema.description}</p>
          )}

          {/* Properties table */}
          <div className="border border-edge rounded overflow-hidden">
            <table className="w-full text-xs">
              <thead>
                <tr className="bg-panel text-muted">
                  <th className="text-left px-3 py-1.5 font-medium">{t('contracts.schemas.property', 'Property')}</th>
                  <th className="text-left px-3 py-1.5 font-medium">{t('contracts.schemas.type', 'Type')}</th>
                  <th className="text-center px-3 py-1.5 font-medium">{t('contracts.schemas.required', 'Req')}</th>
                  <th className="text-left px-3 py-1.5 font-medium">{t('contracts.schemas.description', 'Description')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {schema.properties.map((prop) => (
                  <tr key={prop.name} className="hover:bg-elevated/30">
                    <td className="px-3 py-1.5 font-mono text-body">{prop.name}</td>
                    <td className="px-3 py-1.5">
                      <span className="inline-flex items-center gap-1">
                        {prop.ref ? (
                          <span className="text-accent font-medium">
                            <Tag size={9} className="inline" /> {prop.ref}
                          </span>
                        ) : (
                          <span className="text-muted">{prop.type}{prop.format ? ` (${prop.format})` : ''}</span>
                        )}
                      </span>
                    </td>
                    <td className="px-3 py-1.5 text-center">
                      {prop.required ? (
                        <span className="text-warning font-bold">●</span>
                      ) : (
                        <span className="text-muted/40">○</span>
                      )}
                    </td>
                    <td className="px-3 py-1.5 text-muted truncate max-w-[200px]">{prop.description || '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardBody>
      )}
    </Card>
  );
}

// ── Parser ────────────────────────────────────────────────────────────────────

/**
 * Lightweight schema parser from raw spec content.
 * Extracts schemas from OpenAPI components/schemas or definitions (Swagger 2.x).
 * Returns an enriched list with reference counts.
 */
function parseSchemas(specContent: string, protocol: string): SchemaItem[] {
  if (!specContent?.trim()) return [];

  // WSDL/XML schemas not supported in client-side parsing yet
  if (protocol === 'Wsdl') return [];

  // Only parse JSON-based specs for now
  const trimmed = specContent.trim();
  if (!trimmed.startsWith('{')) return [];

  try {
    const parsed = JSON.parse(trimmed);
    const defs = parsed.components?.schemas ?? parsed.definitions ?? {};
    const allContent = JSON.stringify(parsed);

    const schemas: SchemaItem[] = [];
    for (const [name, schemaDef] of Object.entries(defs)) {
      const def = schemaDef as Record<string, unknown>;
      const properties: SchemaProperty[] = [];
      const requiredFields = Array.isArray(def.required) ? (def.required as string[]) : [];

      if (def.properties && typeof def.properties === 'object') {
        for (const [propName, propDef] of Object.entries(def.properties as Record<string, Record<string, unknown>>)) {
          const ref = (propDef.$ref as string) ?? (propDef.items as Record<string, unknown>)?.$ref as string | undefined;
          properties.push({
            name: propName,
            type: (propDef.type as string) ?? (ref ? '$ref' : 'object'),
            required: requiredFields.includes(propName),
            description: (propDef.description as string) ?? '',
            format: propDef.format as string | undefined,
            ref: ref ? ref.split('/').pop() : undefined,
          });
        }
      }

      // Count how many times this schema is referenced
      const refPatterns = [`#/components/schemas/${name}`, `#/definitions/${name}`];
      const usageCount = refPatterns.reduce((acc, pat) => acc + (allContent.split(pat).length - 1), 0);

      schemas.push({
        name,
        type: (def.type as string) ?? 'object',
        description: (def.description as string) ?? '',
        properties,
        isShared: usageCount > 1,
        usageCount,
      });
    }

    return schemas.sort((a, b) => b.usageCount - a.usageCount);
  } catch {
    return [];
  }
}
