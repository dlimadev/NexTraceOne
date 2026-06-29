import { useState, useCallback, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { BookOpen, Plus, Trash2, GripVertical, ArrowLeft, Save } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { IconButton } from '../../../components/IconButton';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Checkbox } from '../../../components/Checkbox';
import { incidentsApi, type RunbookStepDto } from '../api/incidents';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

interface RunbookDetailResponse {
  runbookId: string;
  title: string;
  summary: string;
  linkedServiceId: string | null;
  linkedIncidentType: string | null;
  steps: RunbookStepDto[];
  preconditions: string[];
  postValidationGuidance: string | null;
  createdBy: string;
  createdAt: string;
  updatedAt: string | null;
}

interface RunbookFormState {
  title: string;
  description: string;
  linkedService: string;
  linkedIncidentType: string;
  maintainedBy: string;
  postNotes: string;
  steps: RunbookStepDto[];
  prerequisites: string[];
}

const emptyState: RunbookFormState = {
  title: '',
  description: '',
  linkedService: '',
  linkedIncidentType: '',
  maintainedBy: '',
  postNotes: '',
  steps: [{ stepOrder: 1, title: '', description: '', isOptional: false }],
  prerequisites: [],
};

/**
 * Visual Runbook Builder — create and edit operational runbooks
 * with structured steps, prerequisites, and service linking.
 * Part of the Operations module — Flow 3 (Incidents + Automation + Reliability).
 */
export function RunbookBuilderPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const navigate = useNavigate();
  const { runbookId } = useParams<{ runbookId?: string }>();
  const queryClient = useQueryClient();
  const isEditing = !!runbookId;

  const [form, setForm] = useState<RunbookFormState>(emptyState);
  const [errors, setErrors] = useState<string[]>([]);

  // Fetch existing runbook for edit mode
  const { data: runbookData, isLoading, isError, refetch } = useQuery({
    queryKey: ['runbooks', 'detail', runbookId, activeEnvironmentId],
    queryFn: () => incidentsApi.getRunbookDetail(runbookId!),
    enabled: isEditing,
  });

  // Populate form when data is loaded (replaces deprecated onSuccess)
  useEffect(() => {
    if (runbookData) {
      const data = runbookData as RunbookDetailResponse;
       
      setForm({
        title: data.title ?? '',
        description: data.summary ?? '',
        linkedService: data.linkedServiceId ?? '',
        linkedIncidentType: data.linkedIncidentType ?? '',
        maintainedBy: data.createdBy ?? '',
        postNotes: data.postValidationGuidance ?? '',
        steps: data.steps?.length
          ? data.steps.map((s: RunbookStepDto) => ({
              stepOrder: s.stepOrder,
              title: s.title,
              description: s.description ?? '',
              isOptional: s.isOptional,
            }))
          : [{ stepOrder: 1, title: '', description: '', isOptional: false }],
        prerequisites: data.preconditions ?? [],
      });
    }
  }, [runbookData]);

  const createMutation = useMutation({
    mutationFn: (data: RunbookFormState) =>
      incidentsApi.createRunbook({
        title: data.title,
        description: data.description,
        linkedService: data.linkedService || undefined,
        linkedIncidentType: data.linkedIncidentType || undefined,
        steps: data.steps.filter((s) => s.title.trim()),
        prerequisites: data.prerequisites.filter((p) => p.trim()),
        postNotes: data.postNotes || undefined,
        maintainedBy: data.maintainedBy,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['runbooks'] });
      navigate('/operations/runbooks');
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: RunbookFormState) =>
      incidentsApi.updateRunbook(runbookId!, {
        title: data.title,
        description: data.description,
        linkedService: data.linkedService || undefined,
        linkedIncidentType: data.linkedIncidentType || undefined,
        steps: data.steps.filter((s) => s.title.trim()),
        prerequisites: data.prerequisites.filter((p) => p.trim()),
        postNotes: data.postNotes || undefined,
        maintainedBy: data.maintainedBy,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['runbooks'] });
      navigate('/operations/runbooks');
    },
  });

  const isSaving = createMutation.isPending || updateMutation.isPending;

  const updateField = useCallback(
    <K extends keyof RunbookFormState>(field: K, value: RunbookFormState[K]) => {
      setForm((prev) => ({ ...prev, [field]: value }));
    },
    [],
  );

  const addStep = useCallback(() => {
    setForm((prev) => ({
      ...prev,
      steps: [
        ...prev.steps,
        { stepOrder: prev.steps.length + 1, title: '', description: '', isOptional: false },
      ],
    }));
  }, []);

  const removeStep = useCallback((index: number) => {
    setForm((prev) => ({
      ...prev,
      steps: prev.steps
        .filter((_, i) => i !== index)
        .map((s, i) => ({ ...s, stepOrder: i + 1 })),
    }));
  }, []);

  const updateStep = useCallback(
    (index: number, field: keyof RunbookStepDto, value: string | boolean | number) => {
      setForm((prev) => ({
        ...prev,
        steps: prev.steps.map((s, i) => (i === index ? { ...s, [field]: value } : s)),
      }));
    },
    [],
  );

  const addPrerequisite = useCallback(() => {
    setForm((prev) => ({
      ...prev,
      prerequisites: [...prev.prerequisites, ''],
    }));
  }, []);

  const removePrerequisite = useCallback((index: number) => {
    setForm((prev) => ({
      ...prev,
      prerequisites: prev.prerequisites.filter((_, i) => i !== index),
    }));
  }, []);

  const updatePrerequisite = useCallback((index: number, value: string) => {
    setForm((prev) => ({
      ...prev,
      prerequisites: prev.prerequisites.map((p, i) => (i === index ? value : p)),
    }));
  }, []);

  const validate = (): boolean => {
    const errs: string[] = [];
    if (!form.title.trim()) errs.push(t('runbooks.builder.errors.titleRequired', 'Title is required'));
    if (!form.description.trim()) errs.push(t('runbooks.builder.errors.descriptionRequired', 'Description is required'));
    if (!form.maintainedBy.trim()) errs.push(t('runbooks.builder.errors.maintainedByRequired', 'Maintained By is required'));
    if (form.steps.length === 0 || !form.steps.some((s) => s.title.trim()))
      errs.push(t('runbooks.builder.errors.stepsRequired', 'At least one step with a title is required'));
    setErrors(errs);
    return errs.length === 0;
  };

  const handleSave = () => {
    if (!validate()) return;
    if (isEditing) {
      updateMutation.mutate(form);
    } else {
      createMutation.mutate(form);
    }
  };

  if (isEditing && isLoading) return <PageLoadingState />;
  if (isEditing && isError) return <PageErrorState onRetry={() => refetch()} />;

  return (
    <PageContainer>
      <PageHeader
        title={isEditing ? t('runbooks.builder.editTitle') : t('runbooks.builder.createTitle')}
        subtitle={t('runbooks.builder.subtitle')}
      />

      <PageSection>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate('/operations/runbooks')}
          className="mb-4"
        >
          <ArrowLeft size={14} className="mr-1" />
          {t('runbooks.builder.back')}
        </Button>

        {errors.length > 0 && (
          <div className="mb-4 p-3 rounded-md bg-critical/10 border border-critical/30">
            <ul className="list-disc list-inside text-sm text-critical">
              {errors.map((e, i) => (
                <li key={i}>{e}</li>
              ))}
            </ul>
          </div>
        )}

        {/* Basic Info */}
        <Card className="mb-4">
          <CardHeader>
            <div className="flex items-center gap-2">
              <BookOpen size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">{t('runbooks.builder.title')}</h3>
            </div>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <TextField
                size="sm"
                label={`${t('runbooks.builder.fieldTitle')} *`}
                value={form.title}
                onChange={(e) => updateField('title', e.target.value)}
                placeholder={t('runbooks.builder.fieldTitlePlaceholder')}
              />
              <TextField
                size="sm"
                label={`${t('runbooks.builder.fieldMaintainedBy')} *`}
                value={form.maintainedBy}
                onChange={(e) => updateField('maintainedBy', e.target.value)}
                placeholder={t('runbooks.builder.fieldMaintainedByPlaceholder')}
              />
              <TextArea
                className="md:col-span-2"
                label={`${t('runbooks.builder.fieldDescription')} *`}
                value={form.description}
                onChange={(e) => updateField('description', e.target.value)}
                placeholder={t('runbooks.builder.fieldDescriptionPlaceholder')}
                rows={3}
              />
              <TextField
                size="sm"
                label={t('runbooks.builder.fieldLinkedService')}
                value={form.linkedService}
                onChange={(e) => updateField('linkedService', e.target.value)}
                placeholder={t('runbooks.builder.fieldLinkedServicePlaceholder')}
              />
              <TextField
                size="sm"
                label={t('runbooks.builder.fieldLinkedIncidentType')}
                value={form.linkedIncidentType}
                onChange={(e) => updateField('linkedIncidentType', e.target.value)}
                placeholder={t('runbooks.builder.fieldLinkedIncidentTypePlaceholder')}
              />
            </div>
          </CardBody>
        </Card>

        {/* Steps */}
        <Card className="mb-4">
          <CardHeader>
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-semibold text-heading">{t('runbooks.builder.stepsSection')}</h3>
              <Button variant="ghost" size="sm" onClick={addStep}>
                <Plus size={14} className="mr-1" />
                {t('runbooks.builder.addStep')}
              </Button>
            </div>
          </CardHeader>
          <CardBody>
            <div className="space-y-3">
              {form.steps.map((step, index) => (
                <div key={index} className="flex gap-3 p-3 rounded-md border border-edge bg-elevated">
                  <div className="flex items-center text-muted">
                    <GripVertical size={14} />
                    <span className="text-xs font-mono ml-1">{index + 1}</span>
                  </div>
                  <div className="flex-1 space-y-2">
                    <TextField
                      size="sm"
                      value={step.title}
                      onChange={(e) => updateStep(index, 'title', e.target.value)}
                      placeholder={t('runbooks.builder.stepTitlePlaceholder')}
                    />
                    <TextArea
                      value={step.description ?? ''}
                      onChange={(e) => updateStep(index, 'description', e.target.value)}
                      placeholder={t('runbooks.builder.stepDescriptionPlaceholder')}
                      rows={2}
                    />
                    <Checkbox
                      label={t('runbooks.builder.stepOptional')}
                      checked={step.isOptional}
                      onChange={(e) => updateStep(index, 'isOptional', e.target.checked)}
                    />
                  </div>
                  {form.steps.length > 1 && (
                    <IconButton
                      variant="ghost"
                      size="sm"
                      className="self-start hover:text-critical"
                      label={t('runbooks.builder.removeStep', 'Remove step')}
                      icon={<Trash2 size={14} />}
                      onClick={() => removeStep(index)}
                    />
                  )}
                </div>
              ))}
            </div>
          </CardBody>
        </Card>

        {/* Prerequisites */}
        <Card className="mb-4">
          <CardHeader>
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-semibold text-heading">{t('runbooks.builder.prerequisitesSection')}</h3>
              <Button variant="ghost" size="sm" onClick={addPrerequisite}>
                <Plus size={14} className="mr-1" />
                {t('runbooks.builder.addPrerequisite')}
              </Button>
            </div>
          </CardHeader>
          <CardBody>
            {form.prerequisites.length === 0 ? (
              <p className="text-xs text-muted italic">
                {t('runbooks.builder.addPrerequisite')}
              </p>
            ) : (
              <div className="space-y-2">
                {form.prerequisites.map((prereq, index) => (
                  <div key={index} className="flex items-center gap-2">
                    <div className="flex-1">
                      <TextField
                        size="sm"
                        value={prereq}
                        onChange={(e) => updatePrerequisite(index, e.target.value)}
                        placeholder={t('runbooks.builder.prerequisitePlaceholder')}
                      />
                    </div>
                    <IconButton
                      variant="ghost"
                      size="sm"
                      className="hover:text-critical"
                      label={t('runbooks.builder.removePrerequisite', 'Remove prerequisite')}
                      icon={<Trash2 size={14} />}
                      onClick={() => removePrerequisite(index)}
                    />
                  </div>
                ))}
              </div>
            )}
          </CardBody>
        </Card>

        {/* Post-Execution Notes */}
        <Card className="mb-4">
          <CardBody>
            <TextArea
              label={t('runbooks.builder.fieldPostNotes')}
              value={form.postNotes}
              onChange={(e) => updateField('postNotes', e.target.value)}
              placeholder={t('runbooks.builder.fieldPostNotesPlaceholder')}
              rows={3}
            />
          </CardBody>
        </Card>

        {/* Actions */}
        <div className="flex items-center justify-end gap-3">
          <Button variant="ghost" onClick={() => navigate('/operations/runbooks')}>
            {t('runbooks.builder.cancel')}
          </Button>
          <Button onClick={handleSave} disabled={isSaving} loading={isSaving}>
            <Save size={14} className="mr-2" />
            {isSaving ? t('runbooks.builder.saving') : t('runbooks.builder.save')}
          </Button>
        </div>
      </PageSection>
    </PageContainer>
  );
}
