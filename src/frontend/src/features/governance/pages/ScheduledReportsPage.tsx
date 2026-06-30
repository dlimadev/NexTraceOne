import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { BarChart3, Download, Plus, Trash2, ToggleLeft, ToggleRight } from 'lucide-react';
import { ExportModal, type ExportFormat } from '../../../components/ExportModal';
import { Modal } from '../../../components/Modal';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { IconButton } from '../../../components/IconButton';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

type Schedule = 'daily' | 'weekly' | 'monthly';
type Format = 'pdf' | 'csv' | 'json';

interface ReportSummary {
  reportId: string;
  name: string;
  reportType: string;
  schedule: Schedule;
  format: Format;
  recipientsJson: string;
  isEnabled: boolean;
  lastSentAt?: string;
  createdAt: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────────

const useScheduledReports = () =>
  useQuery({
    queryKey: ['scheduled-reports'],
    queryFn: () =>
      client
        .get<{ items: ReportSummary[]; totalCount: number }>('/api/v1/scheduled-reports')
        .then((r) => r.data),
  });

const useCreateReport = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      name: string;
      reportType: string;
      filtersJson: string;
      schedule: string;
      recipientsJson: string;
      format: string;
    }) => client.post('/scheduled-reports', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['scheduled-reports'] }),
  });
};

const useToggleReport = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ reportId, enabled }: { reportId: string; enabled: boolean }) =>
      client.patch(`/scheduled-reports/${reportId}/toggle`, { reportId, enabled }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['scheduled-reports'] }),
  });
};

const useDeleteReport = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (reportId: string) => client.delete(`/scheduled-reports/${reportId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['scheduled-reports'] }),
  });
};

// ── Constants ──────────────────────────────────────────────────────────────────

const SCHEDULES: Schedule[] = ['daily', 'weekly', 'monthly'];
const FORMATS: Format[] = ['pdf', 'csv', 'json'];

const FORMAT_BADGE: Record<Format, 'info' | 'neutral' | 'warning'> = {
  pdf: 'info',
  csv: 'neutral',
  json: 'warning',
};

// ── Component ──────────────────────────────────────────────────────────────────

/**
 * ScheduledReportsPage — gestão de relatórios programados por utilizador.
 * Permite criar relatórios com schedule (daily/weekly/monthly), formato e destinatários.
 * Pilar: Governance — Relatórios & Exports Personalizados
 */
export function ScheduledReportsPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useScheduledReports();
  const createReport = useCreateReport();
  const toggleReport = useToggleReport();
  const deleteReport = useDeleteReport();

  const [showBuilder, setShowBuilder] = useState(false);
  const [showExport, setShowExport] = useState(false);
  const [reportName, setReportName] = useState('');
  const [reportType, setReportType] = useState('compliance');
  const [schedule, setSchedule] = useState<Schedule>('weekly');
  const [format, setFormat] = useState<Format>('pdf');
  const [recipients, setRecipients] = useState('');

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const canCreate = reportName.trim().length > 0 && reportType.trim().length > 0;

  const handleCreate = () => {
    if (!canCreate) return;
    const recipientList = recipients
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);
    createReport.mutate(
      {
        name: reportName.trim(),
        reportType: reportType.trim(),
        filtersJson: '{}',
        schedule,
        recipientsJson: JSON.stringify(recipientList),
        format,
      },
      {
        onSuccess: () => {
          setShowBuilder(false);
          setReportName('');
          setReportType('compliance');
          setSchedule('weekly');
          setFormat('pdf');
          setRecipients('');
        },
      }
    );
  };

  const parseRecipients = (json: string): string => {
    try {
      const arr = JSON.parse(json) as string[];
      return arr.join(', ');
    } catch {
      return json;
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('scheduledReports.title')}
        actions={
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={() => setShowExport(true)}>
              <Download className="w-4 h-4 mr-1" />
              {t('export.title')}
            </Button>
            <Button size="sm" onClick={() => setShowBuilder(true)}>
              <Plus className="w-4 h-4 mr-1" />
              {t('scheduledReports.create')}
            </Button>
          </div>
        }
      />

      <PageSection>
        {!data?.items.length ? (
          <EmptyState
            icon={<BarChart3 className="w-8 h-8 text-faded" />}
            title={t('scheduledReports.empty')}
            action={
              <Button size="sm" onClick={() => setShowBuilder(true)}>
                {t('scheduledReports.create')}
              </Button>
            }
          />
        ) : (
          <div className="space-y-3">
            {data.items.map((report) => (
              <Card key={report.reportId}>
                <CardBody>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <IconButton
                        variant="ghost"
                        className="hover:text-accent"
                        icon={report.isEnabled
                          ? <ToggleRight className="w-6 h-6 text-accent" />
                          : <ToggleLeft className="w-6 h-6" />}
                        onClick={() =>
                          toggleReport.mutate({
                            reportId: report.reportId,
                            enabled: !report.isEnabled,
                          })
                        }
                        label={
                          report.isEnabled
                            ? t('scheduledReports.disabled')
                            : t('scheduledReports.enabled')
                        }
                      />
                      <div>
                        <p className="text-sm font-medium text-heading">
                          {report.name}
                        </p>
                        <p className="text-xs text-muted mt-0.5">
                          {t('scheduledReports.schedule')}: {report.schedule} ·{' '}
                          {t('scheduledReports.recipients')}:{' '}
                          {parseRecipients(report.recipientsJson) || '—'}
                        </p>
                        {report.lastSentAt && (
                          <p className="text-xs text-faded mt-0.5">
                            {t('scheduledReports.lastSent')}:{' '}
                            {new Date(report.lastSentAt).toLocaleDateString()}
                          </p>
                        )}
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant={FORMAT_BADGE[report.format]}>
                        {report.format.toUpperCase()}
                      </Badge>
                      <Badge variant={report.isEnabled ? 'success' : 'neutral'}>
                        {report.isEnabled
                          ? t('scheduledReports.enabled')
                          : t('scheduledReports.disabled')}
                      </Badge>
                      <IconButton
                        variant="ghost"
                        size="sm"
                        className="ml-2 hover:text-critical"
                        icon={<Trash2 className="w-4 h-4" />}
                        onClick={() => deleteReport.mutate(report.reportId)}
                        label={t('common.delete')}
                      />
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>

      <Modal
        open={showBuilder}
        onClose={() => {
          setShowBuilder(false);
          setReportName('');
          setReportType('compliance');
          setSchedule('weekly');
          setFormat('pdf');
          setRecipients('');
        }}
        title={t('scheduledReports.create')}
        size="md"
        footer={
          <div className="flex items-center justify-between w-full">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setShowBuilder(false);
                setReportName('');
                setReportType('compliance');
                setSchedule('weekly');
                setFormat('pdf');
                setRecipients('');
              }}
            >
              {t('common.cancel')}
            </Button>
            <Button
              size="sm"
              disabled={!canCreate || createReport.isPending}
              loading={createReport.isPending}
              onClick={handleCreate}
            >
              {t('common.save')}
            </Button>
          </div>
        }
      >
        <div className="space-y-4">
          <TextField
            label={t('common.name')}
            type="text"
            value={reportName}
            onChange={(e) => setReportName(e.target.value)}
          />

          <TextField
            label={t('common.type')}
            type="text"
            value={reportType}
            onChange={(e) => setReportType(e.target.value)}
            placeholder="compliance"
          />

          <div className="grid grid-cols-2 gap-2">
            <Select
              label={t('scheduledReports.schedule')}
              value={schedule}
              onChange={(e) => setSchedule(e.target.value as Schedule)}
              options={SCHEDULES.map((s) => ({ value: s, label: s }))}
            />
            <Select
              label={t('scheduledReports.format')}
              value={format}
              onChange={(e) => setFormat(e.target.value as Format)}
              options={FORMATS.map((f) => ({ value: f, label: f.toUpperCase() }))}
            />
          </div>

          <TextField
            label={`${t('scheduledReports.recipients')} (${t('common.commaSeparated')})`}
            type="text"
            value={recipients}
            onChange={(e) => setRecipients(e.target.value)}
            placeholder="user@example.com, team@example.com"
          />
        </div>
      </Modal>

      <ExportModal
        open={showExport}
        onClose={() => setShowExport(false)}
        columns={[
          { key: 'name', label: t('common.name') },
          { key: 'reportType', label: t('common.type') },
          { key: 'schedule', label: t('scheduledReports.schedule') },
          { key: 'format', label: t('scheduledReports.format') },
          { key: 'recipients', label: t('scheduledReports.recipients') },
          { key: 'enabled', label: t('scheduledReports.enabled') },
          { key: 'lastSentAt', label: t('scheduledReports.lastSent') },
        ]}
        onExport={(fmt: ExportFormat) => {
          // POST /api/v1/export with the selected format
          client.post('/export', {
            entity: 'scheduled-reports',
            format: fmt,
          }).catch(() => null);
        }}
      />
    </PageContainer>
  );
}
