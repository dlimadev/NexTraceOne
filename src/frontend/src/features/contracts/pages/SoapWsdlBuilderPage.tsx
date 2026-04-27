import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Code2, Plus, Trash2 } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

interface SoapOperation {
  id: string;
  name: string;
  inputMessage: string;
  outputMessage: string;
  style: 'document' | 'rpc';
  documentation: string;
}

export function SoapWsdlBuilderPage() {
  const { t } = useTranslation();
  const [serviceName, setServiceName] = useState('MyService');
  const [targetNamespace, setTargetNamespace] = useState('http://example.com/service');
  const [wsdlVersion, setWsdlVersion] = useState<'1.1' | '2.0'>('1.1');
  const [operations, setOperations] = useState<SoapOperation[]>([
    { id: '1', name: 'GetCustomer', inputMessage: 'GetCustomerRequest', outputMessage: 'GetCustomerResponse', style: 'document', documentation: '' },
    { id: '2', name: 'CreateOrder', inputMessage: 'CreateOrderRequest', outputMessage: 'CreateOrderResponse', style: 'document', documentation: '' },
  ]);
  const [selected, setSelected] = useState<string | null>('1');

  const addOperation = () => {
    const id = Date.now().toString();
    setOperations((prev) => [...prev, { id, name: 'NewOperation', inputMessage: 'Request', outputMessage: 'Response', style: 'document', documentation: '' }]);
    setSelected(id);
  };

  const update = (id: string, patch: Partial<SoapOperation>) =>
    setOperations((prev) => prev.map((o) => (o.id === id ? { ...o, ...patch } : o)));

  const active = operations.find((o) => o.id === selected);

  return (
    <PageContainer>
      <PageHeader
        title={t('soapBuilder.title')}
        subtitle={t('soapBuilder.subtitle')}
        actions={<Button size="sm">{t('soapBuilder.publish')}</Button>}
      />
      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          {/* Service config + operation list */}
          <div className="lg:col-span-1 space-y-3">
            <Card>
              <CardBody className="p-4">
                <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-3">
                  {t('soapBuilder.serviceInfo')}
                </h3>
                <div className="space-y-2">
                  <div>
                    <label className="text-xs text-muted-foreground">{t('soapBuilder.serviceName')}</label>
                    <input value={serviceName} onChange={(e) => setServiceName(e.target.value)} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background" />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">{t('soapBuilder.targetNamespace')}</label>
                    <input value={targetNamespace} onChange={(e) => setTargetNamespace(e.target.value)} className="w-full mt-1 px-2 py-1.5 text-xs border rounded bg-background font-mono" />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">{t('soapBuilder.wsdlVersion')}</label>
                    <select value={wsdlVersion} onChange={(e) => setWsdlVersion(e.target.value as '1.1' | '2.0')} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background">
                      <option value="1.1">WSDL 1.1</option>
                      <option value="2.0">WSDL 2.0</option>
                    </select>
                  </div>
                </div>
              </CardBody>
            </Card>

            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <Code2 size={14} className="text-warning" />
                <h3 className="text-sm font-semibold">{t('soapBuilder.operations')}</h3>
                <Badge variant="secondary">{operations.length}</Badge>
              </div>
              <Button size="sm" variant="ghost" onClick={addOperation}>
                <Plus size={12} className="mr-1" />
                {t('soapBuilder.addOperation')}
              </Button>
            </div>

            <div className="space-y-1">
              {operations.map((op) => (
                <button
                  key={op.id}
                  onClick={() => setSelected(op.id)}
                  className={`w-full text-left p-2.5 rounded-md border transition-colors ${
                    selected === op.id ? 'border-accent bg-accent/5' : 'border-transparent hover:bg-muted/40'
                  }`}
                >
                  <p className="text-sm font-medium">{op.name}</p>
                  <p className="text-xs text-muted-foreground font-mono">{op.inputMessage} → {op.outputMessage}</p>
                </button>
              ))}
            </div>
          </div>

          {/* Operation detail */}
          <div className="lg:col-span-2">
            {active ? (
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center justify-between mb-4">
                    <h3 className="text-sm font-semibold">{t('soapBuilder.operationDetail')}</h3>
                    <Button size="sm" variant="ghost" className="text-destructive" onClick={() => {
                      setOperations((prev) => prev.filter((o) => o.id !== active.id));
                      setSelected(operations.find((o) => o.id !== active.id)?.id ?? null);
                    }}>
                      <Trash2 size={12} className="mr-1" />
                      {t('soapBuilder.remove')}
                    </Button>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="text-xs text-muted-foreground">{t('soapBuilder.operationName')}</label>
                      <input value={active.name} onChange={(e) => update(active.id, { name: e.target.value })} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background" />
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">{t('soapBuilder.style')}</label>
                      <select value={active.style} onChange={(e) => update(active.id, { style: e.target.value as 'document' | 'rpc' })} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background">
                        <option value="document">document</option>
                        <option value="rpc">rpc</option>
                      </select>
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">{t('soapBuilder.inputMessage')}</label>
                      <input value={active.inputMessage} onChange={(e) => update(active.id, { inputMessage: e.target.value })} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background font-mono text-xs" />
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">{t('soapBuilder.outputMessage')}</label>
                      <input value={active.outputMessage} onChange={(e) => update(active.id, { outputMessage: e.target.value })} className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background font-mono text-xs" />
                    </div>
                  </div>

                  <div className="mt-3">
                    <label className="text-xs text-muted-foreground">{t('soapBuilder.documentation')}</label>
                    <textarea
                      value={active.documentation}
                      onChange={(e) => update(active.id, { documentation: e.target.value })}
                      rows={4}
                      className="w-full mt-1 px-2 py-1.5 text-sm border rounded bg-background"
                    />
                  </div>
                </CardBody>
              </Card>
            ) : (
              <div className="flex items-center justify-center h-48 text-muted-foreground text-sm">
                {t('soapBuilder.selectOperation')}
              </div>
            )}
          </div>
        </div>

        <div className="mt-4 p-3 rounded-lg border border-warning/30 bg-warning/5 text-xs text-muted-foreground">
          {t('soapBuilder.wsdlBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
