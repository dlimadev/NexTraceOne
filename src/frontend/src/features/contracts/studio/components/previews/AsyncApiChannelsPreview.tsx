import * as yaml from 'js-yaml';
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

interface ParsedChannel {
  id: string;
  address: string;
  protocol: string;
}

function parseChannels(content: string): ParsedChannel[] | null {
  try {
    const doc = yaml.load(content) as {
      channels?: Record<string, { address?: string; bindings?: Record<string, unknown> }>;
    };
    if (!doc?.channels || typeof doc.channels !== 'object') return null;
    const channels = Object.entries(doc.channels).map(([id, ch]) => ({
      id,
      address: ch.address ?? id,
      protocol: ch.bindings ? Object.keys(ch.bindings)[0] ?? 'unknown' : 'unknown',
    }));
    return channels.length > 0 ? channels : null;
  } catch {
    return null;
  }
}

interface AsyncApiChannelsPreviewProps {
  content: string;
}

export function AsyncApiChannelsPreview({ content }: AsyncApiChannelsPreviewProps) {
  const { t } = useTranslation();
  const channels = parseChannels(content);

  if (!channels) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  return (
    <div className="space-y-2" data-testid="async-channels-preview">
      {channels.map((ch) => (
        <div key={ch.id} className="flex items-center gap-2 py-1">
          <span className="w-2 h-2 rounded-full bg-success flex-shrink-0" />
          <span className="text-xs font-mono text-heading flex-1 truncate">{ch.address}</span>
          <Badge variant="neutral" className="text-[10px] font-mono">{ch.protocol}</Badge>
        </div>
      ))}
      <div className="pt-2 border-t border-edge text-xs text-faded">
        {channels.length} {channels.length === 1 ? 'channel' : 'channels'}
      </div>
    </div>
  );
}
