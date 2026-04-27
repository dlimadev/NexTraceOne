import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Zap, Plus, Trash2 } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

type ChannelOperation = 'subscribe' | 'publish';

interface Channel {
  id: string;
  address: string;
  protocol: string;
  operation: ChannelOperation;
  description: string;
  messageSchema: string;
}

const PROTOCOLS = ['kafka', 'amqp', 'sns', 'websocket', 'mqtt', 'nats'];

export function AsyncApiBuilderPage() {
  const { t } = useTranslation();
  const [title, setTitle] = useState('My Event-Driven API');
  const [version, setVersion] = useState('1.0.0');
  const [channels, setChannels] = useState<Channel[]>([
    { id: '1', address: 'user.registered', protocol: 'kafka', operation: 'publish', description: 'Emitted when a user registers', messageSchema: '{}' },
    { id: '2', address: 'order.created', protocol: 'kafka', operation: 'publish', description: 'Emitted when an order is created', messageSchema: '{}' },
  ]);
  const [selected, setSelected] = useState<string | null>('1');

  const addChannel = () => {
    const id = Date.now().toString();
    setChannels((prev) => [...prev, { id, address: 'new.event', protocol: 'kafka', operation: 'publish', description: '', messageSchema: '{}' }]);
    setSelected(id);
  };

  const update = (id: string, patch: Partial<Channel>) =>
    setChannels((prev) => prev.map((c) => (c.id === id ? { ...c, ...patch } : c)));

  const active = channels.find((c) => c.id === selected);

  return (
    <PageContainer>
      <PageHeader
        title={t('asyncApiBuilder.title')}
        subtitle={t('asyncApiBuilder.subtitle')}
        actions={<Button size="sm">{t('asyncApiBuilder.publish')}</Button>}
      />
      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          {/* Channel list */}
          <div className="lg:col-span-1 space-y-3">
            <Card>
              <CardBody className="p-4">
                <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-3">
                  {t('asyncApiBuilder.apiInfo')}
                </h3>
                <div className="space-y-2">
                  <div>
                    <label className="text-xs text-muted-foreground">{t('asyncApiBuilder.apiTitle')}</label>
                    <input value={title} onChange={(e) => setTitle(e.target.value)} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background" />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">{t('asyncApiBuilder.version')}</label>
                    <input value={version} onChange={(e) => setVersion(e.target.value)} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background font-mono" />
                  </div>
                </div>
              </CardBody>
            </Card>

            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <Zap size={14} className="text-success" />
                <h3 className="text-sm font-semibold">{t('asyncApiBuilder.channels')}</h3>
                <Badge variant="secondary">{channels.length}</Badge>
              </div>
              <Button size="sm" variant="ghost" onClick={addChannel}>
                <Plus size={12} className="mr-1" />
                {t('asyncApiBuilder.addChannel')}
              </Button>
            </div>

            <div className="space-y-1">
              {channels.map((ch) => (
                <button
                  key={ch.id}
                  onClick={() => setSelected(ch.id)}
                  className={`w-full text-left p-2.5 rounded-md border transition-colors ${
                    selected === ch.id ? 'border-accent bg-accent/5' : 'border-transparent hover:bg-muted/40'
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <Badge variant={ch.operation === 'publish' ? 'success' : 'info'} className="text-xs w-16 justify-center">
                      {ch.operation}
                    </Badge>
                    <span className="text-xs font-mono truncate">{ch.address}</span>
                  </div>
                  <p className="text-xs text-muted-foreground mt-0.5 truncate">{ch.protocol}</p>
                </button>
              ))}
            </div>
          </div>

          {/* Channel detail */}
          <div className="lg:col-span-2">
            {active ? (
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center justify-between mb-4">
                    <h3 className="text-sm font-semibold">{t('asyncApiBuilder.channelDetail')}</h3>
                    <Button size="sm" variant="ghost" className="text-destructive" onClick={() => {
                      setChannels((prev) => prev.filter((c) => c.id !== active.id));
                      setSelected(channels.find((c) => c.id !== active.id)?.id ?? null);
                    }}>
                      <Trash2 size={12} className="mr-1" />
                      {t('asyncApiBuilder.remove')}
                    </Button>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="text-xs text-muted-foreground">{t('asyncApiBuilder.address')}</label>
                      <input value={active.address} onChange={(e) => update(active.id, { address: e.target.value })} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background font-mono" />
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">{t('asyncApiBuilder.protocol')}</label>
                      <select value={active.protocol} onChange={(e) => update(active.id, { protocol: e.target.value })} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background">
                        {PROTOCOLS.map((p) => <option key={p} value={p}>{p}</option>)}
                      </select>
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">{t('asyncApiBuilder.operation')}</label>
                      <select value={active.operation} onChange={(e) => update(active.id, { operation: e.target.value as ChannelOperation })} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background">
                        <option value="publish">publish</option>
                        <option value="subscribe">subscribe</option>
                      </select>
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">{t('asyncApiBuilder.description')}</label>
                      <input value={active.description} onChange={(e) => update(active.id, { description: e.target.value })} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background" />
                    </div>
                  </div>

                  <div className="mt-3">
                    <label className="text-xs text-muted-foreground">{t('asyncApiBuilder.messageSchema')}</label>
                    <textarea
                      value={active.messageSchema}
                      onChange={(e) => update(active.id, { messageSchema: e.target.value })}
                      rows={8}
                      className="w-full mt-1 px-2 py-1.5 text-xs border rounded bg-background font-mono"
                    />
                  </div>
                </CardBody>
              </Card>
            ) : (
              <div className="flex items-center justify-center h-48 text-muted-foreground text-sm">
                {t('asyncApiBuilder.selectChannel')}
              </div>
            )}
          </div>
        </div>

        <div className="mt-4 p-3 rounded-lg border border-success/30 bg-success/5 text-xs text-muted-foreground">
          {t('asyncApiBuilder.asyncApiBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
