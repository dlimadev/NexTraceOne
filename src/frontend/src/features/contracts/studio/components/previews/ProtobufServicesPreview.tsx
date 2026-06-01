import { useTranslation } from 'react-i18next';
import { Badge } from '../../../../../components/Badge';

interface ParsedService {
  name: string;
  rpcs: string[];
}

function parseProto(content: string): { services: ParsedService[]; messages: string[] } | null {
  try {
    const services: ParsedService[] = [];
    const messages: string[] = [];

    const serviceRegex = /service\s+(\w+)\s*\{([^}]*)\}/gm;
    let m;
    while ((m = serviceRegex.exec(content)) !== null) {
      const rpcs = [...m[2].matchAll(/rpc\s+(\w+)/g)].map((r) => r[1]);
      services.push({ name: m[1], rpcs });
    }

    const msgRegex = /message\s+(\w+)/g;
    while ((m = msgRegex.exec(content)) !== null) {
      messages.push(m[1]);
    }

    if (services.length === 0 && messages.length === 0) return null;
    return { services, messages };
  } catch {
    return null;
  }
}

interface ProtobufServicesPreviewProps {
  content: string;
}

export function ProtobufServicesPreview({ content }: ProtobufServicesPreviewProps) {
  const { t } = useTranslation();
  const parsed = parseProto(content);

  if (!parsed) {
    return (
      <div className="flex flex-col items-center justify-center h-32 text-muted" data-testid="preview-empty">
        <span className="text-xs">{t('contractBuilder.preview.empty')}</span>
      </div>
    );
  }

  return (
    <div className="space-y-3" data-testid="protobuf-services-preview">
      {parsed.services.length > 0 && (
        <div>
          <div className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">
            Services ({parsed.services.length})
          </div>
          {parsed.services.map((svc) => (
            <div key={svc.name} className="mb-2">
              <div className="text-xs font-mono font-semibold text-heading">{svc.name}</div>
              <div className="pl-3 space-y-0.5 mt-1">
                {svc.rpcs.map((rpc) => (
                  <div key={rpc} className="flex items-center gap-1.5">
                    <Badge variant="neutral" className="text-[10px]">rpc</Badge>
                    <span className="text-xs font-mono text-muted">{rpc}</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
      {parsed.messages.length > 0 && (
        <div>
          <div className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">
            Messages ({parsed.messages.length})
          </div>
          <div className="flex flex-wrap gap-1.5">
            {parsed.messages.map((msg) => (
              <Badge key={msg} variant="neutral" className="text-[10px] font-mono">{msg}</Badge>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
