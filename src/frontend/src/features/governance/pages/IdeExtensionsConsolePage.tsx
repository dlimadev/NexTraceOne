import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Code2, Users, Download, CheckCircle2, AlertTriangle, Settings } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import client from '../../../api/client';

interface IdeExtension {
  id: string;
  ide: 'vscode' | 'visualstudio' | 'jetbrains';
  version: string;
  installedCount: number;
  enrolledDevelopers: number;
  latestVersion: string;
  status: 'UpToDate' | 'UpdateAvailable' | 'Deprecated';
  features: string[];
}

interface DeveloperEnrollment {
  userId: string;
  displayName: string;
  email: string;
  ide: string;
  extensionVersion: string;
  lastSeen: string;
  status: 'Active' | 'Inactive' | 'Pending';
}

const useIdeConsole = () =>
  useQuery({
    queryKey: ['ide-extensions-console'],
    queryFn: () =>
      client
        .get<{ extensions: IdeExtension[]; enrollments: DeveloperEnrollment[]; isSimulated: boolean }>(
          '/api/v1/platform/ide-extensions',
          { params: { tenantId: 'default' } }
        )
        .then((r) => r.data),
  });

const IDE_ICONS: Record<string, React.ReactNode> = {
  vscode: <Code2 size={16} className="text-info" />,
  visualstudio: <Code2 size={16} className="text-primary" />,
  jetbrains: <Code2 size={16} className="text-warning" />,
};

const IDE_LABELS: Record<string, string> = {
  vscode: 'VS Code',
  visualstudio: 'Visual Studio',
  jetbrains: 'JetBrains',
};

export function IdeExtensionsConsolePage() {
  const { t } = useTranslation();
  const { data, isLoading } = useIdeConsole();

  return (
    <PageContainer>
      <PageHeader
        title={t('ideConsole.title')}
        subtitle={t('ideConsole.subtitle')}
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            {/* Extensions */}
            <h3 className="text-sm font-semibold mb-3">{t('ideConsole.extensions')}</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-6">
              {(data?.extensions ?? []).map((ext) => (
                <Card key={ext.id}>
                  <CardBody className="p-4">
                    <div className="flex items-center gap-2 mb-3">
                      {IDE_ICONS[ext.ide]}
                      <div>
                        <p className="text-sm font-semibold">{IDE_LABELS[ext.ide] ?? ext.ide}</p>
                        <p className="text-xs text-muted-foreground font-mono">{ext.version}</p>
                      </div>
                      <Badge
                        variant={ext.status === 'UpToDate' ? 'success' : ext.status === 'UpdateAvailable' ? 'warning' : 'secondary'}
                        className="ml-auto text-xs"
                      >
                        {ext.status}
                      </Badge>
                    </div>

                    <div className="grid grid-cols-2 gap-2 text-xs mb-3">
                      <div className="flex items-center gap-1">
                        <Download size={10} className="text-muted-foreground" />
                        <span className="text-muted-foreground">{t('ideConsole.installed')}</span>
                        <span className="font-bold ml-auto">{ext.installedCount}</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <Users size={10} className="text-muted-foreground" />
                        <span className="text-muted-foreground">{t('ideConsole.enrolled')}</span>
                        <span className="font-bold ml-auto">{ext.enrolledDevelopers}</span>
                      </div>
                    </div>

                    <div className="flex flex-wrap gap-1 mb-3">
                      {ext.features.map((f) => (
                        <Badge key={f} variant="secondary" className="text-xs">{f}</Badge>
                      ))}
                    </div>

                    <div className="flex gap-2">
                      {ext.status === 'UpdateAvailable' && (
                        <Button size="sm" variant="ghost" className="flex-1 text-xs">
                          <Download size={10} className="mr-1" />
                          {t('ideConsole.pushUpdate')} {ext.latestVersion}
                        </Button>
                      )}
                      <Button size="sm" variant="ghost" className="text-xs">
                        <Settings size={10} className="mr-1" />
                        {t('ideConsole.configure')}
                      </Button>
                    </div>
                  </CardBody>
                </Card>
              ))}
            </div>

            {/* Developer enrollments */}
            <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <Users size={14} />
              {t('ideConsole.developerEnrollments')}
            </h3>
            <div className="space-y-2">
              {(data?.enrollments ?? []).map((dev) => (
                <Card key={dev.userId}>
                  <CardBody className="p-3">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          <p className="text-sm font-medium">{dev.displayName}</p>
                          <Badge variant="outline" className="text-xs">{IDE_LABELS[dev.ide] ?? dev.ide}</Badge>
                          <Badge variant="secondary" className="text-xs font-mono">{dev.extensionVersion}</Badge>
                        </div>
                        <p className="text-xs text-muted-foreground">{dev.email} · {t('ideConsole.lastSeen')}: {new Date(dev.lastSeen).toLocaleString()}</p>
                      </div>
                      <Badge
                        variant={dev.status === 'Active' ? 'success' : dev.status === 'Pending' ? 'warning' : 'secondary'}
                      >
                        {dev.status === 'Active' ? <CheckCircle2 size={10} className="mr-1" /> : <AlertTriangle size={10} className="mr-1" />}
                        {dev.status}
                      </Badge>
                    </div>
                  </CardBody>
                </Card>
              ))}
              {(data?.enrollments ?? []).length === 0 && (
                <div className="text-center p-8 text-muted-foreground text-sm">
                  {t('ideConsole.noEnrollments')}
                </div>
              )}
            </div>

            <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
              {t('sotCenter.simulatedBanner')}
            </div>
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
