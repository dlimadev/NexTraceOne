import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Key, Package, AlertTriangle, CheckCircle2, Download, Wifi, WifiOff } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import client from '../../../api/client';

interface LicenseEntitlement {
  id: string;
  module: string;
  seats: number;
  usedSeats: number;
  expiresAt: string;
  tier: 'Community' | 'Pro' | 'Enterprise' | 'AirGapped';
  features: string[];
  status: 'Active' | 'Expiring' | 'Expired';
}

interface LicenseInfo {
  licenseKey: string;
  tenant: string;
  mode: 'online' | 'airgapped';
  entitlements: LicenseEntitlement[];
  lastValidated: string;
  isSimulated: boolean;
}

const useLicensing = () =>
  useQuery({
    queryKey: ['licensing-admin'],
    queryFn: () =>
      client
        .get<LicenseInfo>('/api/v1/platform/licensing', { params: { tenantId: 'default' } })
        .then((r) => r.data),
  });

const STATUS_CONFIG = {
  Active: { badge: 'success' as const, icon: <CheckCircle2 size={12} /> },
  Expiring: { badge: 'warning' as const, icon: <AlertTriangle size={12} /> },
  Expired: { badge: 'destructive' as const, icon: <AlertTriangle size={12} /> },
};

export function LicensingAdminPage() {
  const { t } = useTranslation();
  const { data, isLoading } = useLicensing();

  return (
    <PageContainer>
      <PageHeader
        title={t('licensing.title')}
        subtitle={t('licensing.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button size="sm" variant="ghost">
              <Download size={14} className="mr-1" />
              {t('licensing.exportOfflinePack')}
            </Button>
            <Button size="sm">{t('licensing.renewLicense')}</Button>
          </div>
        }
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : data ? (
          <>
            {/* License header card */}
            <Card className="mb-6">
              <CardBody className="p-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <Key size={14} className="text-accent" />
                      <h2 className="text-sm font-semibold">{data.tenant}</h2>
                      <Badge variant={data.mode === 'online' ? 'success' : 'secondary'} className="text-xs flex items-center gap-1">
                        {data.mode === 'online' ? <Wifi size={10} /> : <WifiOff size={10} />}
                        {data.mode}
                      </Badge>
                    </div>
                    <p className="text-xs font-mono text-muted-foreground">{data.licenseKey}</p>
                    <p className="text-xs text-muted-foreground mt-1">
                      {t('licensing.lastValidated')}: {new Date(data.lastValidated).toLocaleString()}
                    </p>
                  </div>
                  <Button size="sm" variant="ghost">
                    {t('licensing.validate')}
                  </Button>
                </div>
              </CardBody>
            </Card>

            {/* Entitlements */}
            <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <Package size={14} />
              {t('licensing.entitlements')} ({data.entitlements.length})
            </h3>
            <div className="space-y-3">
              {data.entitlements.map((ent) => {
                const cfg = STATUS_CONFIG[ent.status] ?? STATUS_CONFIG.Active;
                const seatPct = ent.seats > 0 ? (ent.usedSeats / ent.seats) * 100 : 0;
                return (
                  <Card key={ent.id}>
                    <CardBody className="p-4">
                      <div className="flex items-start justify-between gap-3 mb-3">
                        <div>
                          <div className="flex items-center gap-2">
                            <h4 className="text-sm font-semibold">{ent.module}</h4>
                            <Badge variant="secondary" className="text-xs">{ent.tier}</Badge>
                          </div>
                          <p className="text-xs text-muted-foreground mt-0.5">
                            {t('licensing.expires')}: {new Date(ent.expiresAt).toLocaleDateString()}
                          </p>
                        </div>
                        <Badge variant={cfg.badge} className="flex items-center gap-1 text-xs">
                          {cfg.icon}
                          {ent.status}
                        </Badge>
                      </div>

                      {/* Seats */}
                      <div className="mb-3">
                        <div className="flex justify-between text-xs text-muted-foreground mb-1">
                          <span>{t('licensing.seats')}</span>
                          <span>{ent.usedSeats} / {ent.seats === -1 ? '∞' : ent.seats}</span>
                        </div>
                        {ent.seats !== -1 && (
                          <div className="w-full bg-muted rounded-full h-1.5">
                            <div
                              className={`h-1.5 rounded-full ${seatPct > 90 ? 'bg-destructive' : seatPct > 70 ? 'bg-warning' : 'bg-success'}`}
                              style={{ width: `${Math.min(seatPct, 100)}%` }}
                            />
                          </div>
                        )}
                      </div>

                      {/* Features */}
                      <div className="flex flex-wrap gap-1">
                        {ent.features.map((f) => (
                          <Badge key={f} variant="secondary" className="text-xs">{f}</Badge>
                        ))}
                      </div>
                    </CardBody>
                  </Card>
                );
              })}
            </div>

            <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
              {t('sotCenter.simulatedBanner')}
            </div>
          </>
        ) : null}
      </PageSection>
    </PageContainer>
  );
}
