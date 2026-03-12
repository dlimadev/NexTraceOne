import client from './client';
import type { WorkflowTemplate, WorkflowInstance, PagedList } from '../types';

export const workflowApi = {
  listTemplates: () =>
    client.get<WorkflowTemplate[]>('/workflow/templates').then((r) => r.data),

  getTemplate: (id: string) =>
    client.get<WorkflowTemplate>(`/workflow/templates/${id}`).then((r) => r.data),

  listInstances: (page = 1, pageSize = 20) =>
    client
      .get<PagedList<WorkflowInstance>>('/workflow/instances', { params: { page, pageSize } })
      .then((r) => r.data),

  getInstance: (id: string) =>
    client.get<WorkflowInstance>(`/workflow/instances/${id}`).then((r) => r.data),

  approve: (instanceId: string, stageId: string, comment?: string) =>
    client
      .post(`/workflow/instances/${instanceId}/stages/${stageId}/approve`, { comment })
      .then((r) => r.data),

  reject: (instanceId: string, stageId: string, reason: string) =>
    client
      .post(`/workflow/instances/${instanceId}/stages/${stageId}/reject`, { reason })
      .then((r) => r.data),

  requestChanges: (instanceId: string, stageId: string, comment: string) =>
    client
      .post(`/workflow/instances/${instanceId}/stages/${stageId}/request-changes`, { comment })
      .then((r) => r.data),
};
