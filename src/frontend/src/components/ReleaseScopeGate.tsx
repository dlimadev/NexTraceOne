import type { ReactNode } from 'react';
import { ShieldAlert } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardBody, CardHeader } from './Card';
import { PageContainer } from './shell';

interface ReleaseScopeGateProps {
  moduleKey: string;
  children?: ReactNode;
}

export function ReleaseScopeGate({ moduleKey }: ReleaseScopeGateProps) {
  const { t } = useTranslation();

  return (
    <PageContainer>
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2 text-warning">
            <ShieldAlert size={16} />
            <h1 className="text-sm font-semibold text-heading">
              {t('releaseScope.excludedTitle')}
            </h1>
          </div>
        </CardHeader>
        <CardBody>
          <p className="text-sm text-body">
            {t('releaseScope.excludedDescription')}
          </p>
          <p className="mt-3 text-xs text-muted">
            {t(`releaseScope.modules.${moduleKey}`)}
          </p>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
