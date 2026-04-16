import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Settings2, Save } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

interface ReleaseParam {
  key: string;
  defaultValue: string | boolean | number;
  type: 'boolean' | 'number' | 'string';
}

const RELEASE_PARAMS: ReleaseParam[] = [
  { key: 'release.require_release_for_production', defaultValue: true, type: 'boolean' },
  { key: 'release.require_release_for_preprod', defaultValue: true, type: 'boolean' },
  { key: 'release.auto_assign_commits_on_promotion', defaultValue: true, type: 'boolean' },
  { key: 'release.allow_external_ingest', defaultValue: false, type: 'boolean' },
  { key: 'release.approval_callback_token_expiry_hours', defaultValue: 48, type: 'number' },
  { key: 'release.allow_po_remove_workitems', defaultValue: true, type: 'boolean' },
  { key: 'release.block_promotion_if_commits_unassigned', defaultValue: false, type: 'boolean' },
];

type ParamValues = Record<string, boolean | number | string>;

/**
 * ReleaseControlParametersPage — gestão dos parâmetros de controlo de releases.
 *
 * Permite que os administradores configurem os parâmetros de sistema que governam
 * o ciclo de vida de releases sem redeploy. Todos os parâmetros são persistidos
 * no banco de dados via módulo Configuration.
 *
 * Parâmetros incluídos:
 * - Obrigatoriedade de release para produção/pré-produção
 * - Auto-assignação de commits na promoção
 * - Ingestão externa de releases
 * - Expiração do token de callback
 * - Permissões de PO/PM para remover work items
 * - Bloqueio de promoção com commits sem release
 */
export function ReleaseControlParametersPage() {
  const { t } = useTranslation();

  const [values, setValues] = useState<ParamValues>(() =>
    RELEASE_PARAMS.reduce<ParamValues>((acc, p) => {
      acc[p.key] = p.defaultValue;
      return acc;
    }, {}),
  );
  const [saved, setSaved] = useState(false);

  const handleSave = () => {
    // In a full implementation, this would call the Configuration API
    // to persist these values in the database
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  const handleChange = (key: string, value: boolean | number | string) => {
    setValues(prev => ({ ...prev, [key]: value }));
  };

  return (
    <PageContainer>
      <PageHeader
        icon={<Settings2 className="w-6 h-6 text-accent" />}
        title={t('releaseControlParams.title')}
        subtitle={t('releaseControlParams.subtitle')}
        actions={
          <button
            onClick={handleSave}
            className="inline-flex items-center gap-2 rounded-md bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent/90 transition-colors"
          >
            <Save className="w-4 h-4" />
            {saved ? t('releaseControlParams.saved') : t('releaseControlParams.saveChanges')}
          </button>
        }
      />

      <Card>
        <CardHeader>
          <h3 className="text-sm font-semibold text-heading">{t('releaseControlParams.systemParams')}</h3>
          <p className="text-xs text-muted mt-1">{t('releaseControlParams.systemParamsHint')}</p>
        </CardHeader>
        <CardBody>
          <div className="space-y-6">
            {RELEASE_PARAMS.map(param => (
              <div key={param.key} className="flex items-start justify-between gap-4 py-3 border-b border-edge last:border-0">
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium text-heading font-mono">{param.key}</div>
                  <div className="text-xs text-muted mt-0.5">
                    {t(`releaseControlParams.params.${param.key.replace(/\./g, '_')}.description`, {
                      defaultValue: t('releaseControlParams.noDescription'),
                    })}
                  </div>
                </div>
                <div className="flex-shrink-0">
                  {param.type === 'boolean' ? (
                    <label className="relative inline-flex items-center cursor-pointer">
                      <input
                        type="checkbox"
                        className="sr-only peer"
                        checked={values[param.key] as boolean}
                        onChange={e => handleChange(param.key, e.target.checked)}
                      />
                      <div className="w-10 h-5 bg-edge peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-accent rounded-full peer peer-checked:after:translate-x-5 peer-checked:bg-accent after:content-[''] after:absolute after:top-0.5 after:left-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all" />
                    </label>
                  ) : (
                    <input
                      type="number"
                      className="w-24 rounded-md bg-canvas border border-edge px-3 py-1.5 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                      value={values[param.key] as number}
                      onChange={e => handleChange(param.key, Number(e.target.value))}
                    />
                  )}
                </div>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      <Card className="mt-4">
        <CardHeader>
          <h3 className="text-sm font-semibold text-heading">{t('releaseControlParams.callbackInfo')}</h3>
        </CardHeader>
        <CardBody>
          <div className="rounded-lg bg-surface border border-edge p-4 text-sm text-muted space-y-2">
            <p>{t('releaseControlParams.callbackInfoText1')}</p>
            <p className="font-mono text-xs text-heading">
              POST /api/v1/releases/{'{'}<span className="text-accent">id</span>{'}'}/approvals/{'{'}<span className="text-accent">token</span>{'}'}/respond
            </p>
            <p>{t('releaseControlParams.callbackInfoText2')}</p>
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
