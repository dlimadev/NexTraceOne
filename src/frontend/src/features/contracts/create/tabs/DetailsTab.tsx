import { useTranslation } from 'react-i18next';
import { TextField, TextArea, Select } from '../../../../shared/ui';
import type { ContractProtocol } from '../../types';
import type { useContractDraftForm } from '../useContractDraftForm';

interface DetailsTabProps {
  form: ReturnType<typeof useContractDraftForm>;
}

/**
 * Formulário de detalhes do contrato (nome, descrição, protocolo, conteúdo de
 * importação / prompt de IA e metadados específicos por tipo: SOAP, Event, Background Service).
 * Componente presentacional: lê valores/setters do objecto `form` (hook).
 */
export function DetailsTab({ form }: DetailsTabProps) {
  const { t } = useTranslation();

  return (
    <div className="rounded-xl border border-edge bg-card divide-y divide-edge">
      {/* Name */}
      <div className="p-4">
        <TextField
          label={`${t('contracts.create.name', 'Name')} *`}
          value={form.title}
          onChange={form.setField('title')}
          placeholder={t('contracts.create.namePlaceholder', 'e.g., User Management API')}
        />
      </div>

      {/* Description */}
      <div className="p-4">
        <TextArea
          label={t('contracts.create.description', 'Description')}
          value={form.description}
          onChange={form.setField('description')}
          rows={3}
          helperText={t('common.optional', 'optional')}
          placeholder={t('contracts.create.descriptionPlaceholder', 'Describe what this service does...')}
        />
      </div>

      {/* Protocol — only when multiple choices */}
      {form.protocols.length > 1 && (
        <div className="p-4">
          <Select
            label={`${t('contracts.selectProtocol', 'Protocol')} *`}
            value={form.selectedProtocol}
            onChange={(e) => form.setSelectedProtocol(e.target.value as ContractProtocol)}
            placeholder={t('contracts.selectProtocol', 'Select Protocol')}
            options={form.protocols.map((p) => ({ value: p, label: t(`contracts.protocols.${p}`, p) }))}
          />
        </div>
      )}

      {/* Import spec content */}
      {form.selectedMode === 'import' && (
        <div className="p-4">
          <TextArea
            label={`${t('contracts.create.importContent', 'Specification Content')} *`}
            value={form.importContent}
            onChange={form.setField('importContent')}
            rows={10}
            textareaClassName="font-mono text-xs"
            helperText={
              form.isSoapType
                ? t('contracts.create.wsdlXmlHint', 'WSDL XML')
                : form.isEventType
                  ? t('contracts.create.asyncApiJsonHint', 'AsyncAPI JSON')
                  : undefined
            }
            placeholder={
              form.isSoapType
                ? t(
                    'contracts.create.wsdlPlaceholder',
                    'Paste your WSDL XML here (<?xml version="1.0"?><definitions ...>)...',
                  )
                : form.isEventType
                  ? t(
                      'contracts.create.asyncApiPlaceholder',
                      'Paste your AsyncAPI JSON here ({"asyncapi":"2.6.0","info":{"title":"..."},...})...',
                    )
                  : t('contracts.specContentPlaceholder', 'Paste your specification here (JSON/YAML/XML)...')
            }
          />
        </div>
      )}

      {/* AI prompt */}
      {form.selectedMode === 'ai' && (
        <div className="p-4">
          <TextArea
            label={`${t('contracts.create.aiPrompt', 'AI Prompt')} *`}
            value={form.aiPrompt}
            onChange={form.setField('aiPrompt')}
            rows={6}
            placeholder={t(
              'contracts.create.aiPromptPlaceholder',
              'Describe the API you want to generate. Include endpoints, operations, data models...',
            )}
            helperText={t(
              'contracts.create.aiPromptHint',
              'The AI will generate a specification draft based on your description. You can refine it in the studio after creation.',
            )}
          />
        </div>
      )}

      {/* SOAP-specific metadata */}
      {form.isSoapType && (
        <div className="p-4 space-y-3">
          <p className="text-[10px] text-muted font-semibold uppercase tracking-wider">
            {t('contracts.create.soapMetadata', 'SOAP Service Metadata')}
          </p>

          <TextField
            label={t('contracts.create.soapServiceName', 'Service Name')}
            value={form.soapServiceName}
            onChange={form.setField('soapServiceName')}
            placeholder={t('contracts.create.soapServiceNamePlaceholder', 'e.g., UserService')}
          />

          <TextField
            label={t('contracts.create.soapTargetNamespace', 'Target Namespace')}
            value={form.soapTargetNamespace}
            onChange={form.setField('soapTargetNamespace')}
            placeholder="http://example.com/service"
          />

          <div className="grid grid-cols-2 gap-3">
            <Select
              label={t('contracts.create.soapVersion', 'SOAP Version')}
              value={form.soapVersion}
              onChange={(e) => form.setSoapVersion(e.target.value as '1.1' | '1.2')}
              options={[
                { value: '1.1', label: 'SOAP 1.1' },
                { value: '1.2', label: 'SOAP 1.2' },
              ]}
            />
            <TextField
              label={`${t('contracts.create.soapEndpointUrl', 'Endpoint URL')} ${t('common.optional', '(optional)')}`}
              value={form.soapEndpointUrl}
              onChange={form.setField('soapEndpointUrl')}
              placeholder="http://example.com/service"
            />
          </div>
        </div>
      )}

      {/* Event/AsyncAPI metadata */}
      {form.isEventType && (
        <div className="p-4 space-y-3">
          <p className="text-[10px] text-muted font-semibold uppercase tracking-wider">
            {t('contracts.create.asyncApiMetadata', 'AsyncAPI Event Metadata')}
          </p>
          <div className="grid grid-cols-2 gap-3">
            <Select
              label={t('contracts.create.asyncApiVersion', 'AsyncAPI Version')}
              value={form.asyncApiVersion}
              onChange={form.setField('asyncApiVersion')}
              options={[
                { value: '2.6.0', label: 'AsyncAPI 2.6.0' },
                { value: '3.0.0', label: 'AsyncAPI 3.0.0' },
              ]}
            />
            <Select
              label={t('contracts.create.defaultContentType', 'Default Content Type')}
              value={form.defaultContentType}
              onChange={form.setField('defaultContentType')}
              options={[
                { value: 'application/json', label: 'application/json' },
                { value: 'application/avro', label: 'application/avro' },
                { value: 'application/protobuf', label: 'application/protobuf' },
              ]}
            />
          </div>
        </div>
      )}

      {/* Background Service metadata */}
      {form.isBackgroundServiceType && (
        <div className="p-4 space-y-3">
          <p className="text-[10px] text-muted font-semibold uppercase tracking-wider">
            {t('contracts.create.bgServiceMetadata', 'Background Service Metadata')}
          </p>

          <TextField
            label={`${t('contracts.create.bgServiceName', 'Service / Job Name')} *`}
            value={form.bgServiceName}
            onChange={form.setField('bgServiceName')}
            placeholder={t(
              'contracts.create.bgServiceNamePlaceholder',
              'e.g., OrderExpirationJob, ReportGeneratorWorker',
            )}
          />

          <div className="grid grid-cols-2 gap-3">
            <Select
              label={t('contracts.create.bgCategory', 'Category')}
              value={form.bgCategory}
              onChange={form.setField('bgCategory')}
              options={[
                { value: 'Job', label: t('contracts.create.bgCategoryJob', 'Job') },
                { value: 'Worker', label: t('contracts.create.bgCategoryWorker', 'Worker') },
                { value: 'Scheduler', label: t('contracts.create.bgCategoryScheduler', 'Scheduler') },
                { value: 'Processor', label: t('contracts.create.bgCategoryProcessor', 'Processor') },
                { value: 'Exporter', label: t('contracts.create.bgCategoryExporter', 'Exporter') },
                { value: 'Notifier', label: t('contracts.create.bgCategoryNotifier', 'Notifier') },
              ]}
            />
            <Select
              label={t('contracts.create.bgTriggerType', 'Trigger Type')}
              value={form.bgTriggerType}
              onChange={form.setField('bgTriggerType')}
              options={[
                { value: 'OnDemand', label: t('contracts.create.bgTriggerOnDemand', 'On Demand') },
                { value: 'Cron', label: t('contracts.create.bgTriggerCron', 'Cron') },
                { value: 'Interval', label: t('contracts.create.bgTriggerInterval', 'Interval') },
                { value: 'EventTriggered', label: t('contracts.create.bgTriggerEventTriggered', 'Event Triggered') },
                { value: 'Continuous', label: t('contracts.create.bgTriggerContinuous', 'Continuous') },
              ]}
            />
          </div>

          {(form.bgTriggerType === 'Cron' || form.bgTriggerType === 'Interval') && (
            <TextField
              label={t('contracts.create.bgScheduleExpression', 'Schedule Expression')}
              value={form.bgScheduleExpression}
              onChange={form.setField('bgScheduleExpression')}
              placeholder={
                form.bgTriggerType === 'Cron'
                  ? t('contracts.create.bgCronPlaceholder', 'e.g., 0 * * * * (every hour)')
                  : t('contracts.create.bgIntervalPlaceholder', 'e.g., PT5M (ISO 8601 interval)')
              }
            />
          )}
        </div>
      )}
    </div>
  );
}
