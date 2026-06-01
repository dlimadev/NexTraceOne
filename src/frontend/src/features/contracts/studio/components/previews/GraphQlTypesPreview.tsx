import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

interface ParsedType {
  kind: string;
  name: string;
  fieldCount: number;
}

function parseTypes(sdl: string): ParsedType[] | null {
  try {
    const regex = /(?:^|\n)(type|input|enum|interface|union)\s+(\w+)[^{]*\{([^}]*)\}/gm;
    const types: ParsedType[] = [];
    let match;
    while ((match = regex.exec(sdl)) !== null) {
      const [, kind, name, body] = match;
      const fields = body
        .split('\n')
        .map((l) => l.trim())
        .filter((l) => l && !l.startsWith('#') && !l.startsWith('"""'));
      if (name !== 'Query' && name !== 'Mutation' && name !== 'Subscription') {
        types.push({ kind, name, fieldCount: fields.length });
      } else {
        types.push({ kind: 'operation', name, fieldCount: fields.length });
      }
    }
    return types.length > 0 ? types : null;
  } catch {
    return null;
  }
}

const KIND_VARIANT: Record<string, NonNullable<import('../../../../../components/Badge').BadgeProps['variant']>> = {
  type: 'default',
  input: 'info',
  enum: 'warning',
  interface: 'neutral',
  union: 'neutral',
  operation: 'success',
};

interface GraphQlTypesPreviewProps {
  content: string;
}

export function GraphQlTypesPreview({ content }: GraphQlTypesPreviewProps) {
  const { t } = useTranslation();
  const types = parseTypes(content);

  if (!types) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  return (
    <div className="space-y-1.5" data-testid="graphql-types-preview">
      {types.map((tp) => (
        <div key={tp.name} className="flex items-center gap-2">
          <Badge
            variant={KIND_VARIANT[tp.kind] ?? 'neutral'}
            className="text-[10px] w-16 justify-center flex-shrink-0"
          >
            {tp.kind}
          </Badge>
          <span className="text-xs font-mono text-heading flex-1">{tp.name}</span>
          <span className="text-xs text-faded">{tp.fieldCount} fields</span>
        </div>
      ))}
      <div className="pt-2 border-t border-edge text-xs text-faded">
        {types.length} {types.length === 1 ? 'type' : 'types'}
      </div>
    </div>
  );
}
