import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

function parseOperations(xml: string): string[] | null {
  try {
    const ops: string[] = [];
    const regex = /<(?:wsdl:)?operation\s+name="([^"]+)"/g;
    let m;
    while ((m = regex.exec(xml)) !== null) {
      ops.push(m[1]);
    }
    const unique = [...new Set(ops)];
    return unique.length > 0 ? unique : null;
  } catch {
    return null;
  }
}

interface SoapOperationsPreviewProps {
  content: string;
}

export function SoapOperationsPreview({ content }: SoapOperationsPreviewProps) {
  const { t } = useTranslation();
  const operations = parseOperations(content);

  if (!operations) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  return (
    <div className="space-y-1.5" data-testid="soap-operations-preview">
      <div className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">
        Operations ({operations.length})
      </div>
      {operations.map((op) => (
        <div key={op} className="flex items-center gap-2">
          <Badge variant="warning" className="text-[10px] flex-shrink-0">op</Badge>
          <span className="text-xs font-mono text-heading">{op}</span>
        </div>
      ))}
    </div>
  );
}
