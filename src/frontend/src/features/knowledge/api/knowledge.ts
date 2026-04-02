import client from '../../../api/client';
import type {
  KnowledgeDocumentsListResponse,
  KnowledgeDocumentDetail,
  OperationalNotesListResponse,
  KnowledgeSearchResponse,
  KnowledgeRelationDto,
  KnowledgeDocumentRelationItem,
  OperationalNoteRelationItem,
  CreateKnowledgeDocumentRequest,
  CreateOperationalNoteRequest,
  CreateKnowledgeRelationRequest,
  DocumentCategory,
  DocumentStatus,
  NoteSeverity,
  KnowledgeRelationType,
} from '../../../types';

export const knowledgeApi = {
  listDocuments: (params: {
    category?: DocumentCategory;
    status?: DocumentStatus;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<KnowledgeDocumentsListResponse>('/knowledge/documents', { params }),

  getDocumentById: (documentId: string) =>
    client.get<KnowledgeDocumentDetail>(`/knowledge/documents/${documentId}`),

  listOperationalNotes: (params: {
    severity?: NoteSeverity;
    contextType?: string;
    contextEntityId?: string;
    isResolved?: boolean;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<OperationalNotesListResponse>('/knowledge/operational-notes', { params }),

  search: (params: { q: string; scope?: string; maxResults?: number }) =>
    client.get<KnowledgeSearchResponse>('/knowledge/search', { params }),

  createDocument: (data: CreateKnowledgeDocumentRequest) =>
    client.post<{ documentId: string }>('/knowledge/documents', data),

  createOperationalNote: (data: CreateOperationalNoteRequest) =>
    client.post<{ noteId: string }>('/knowledge/operational-notes', data),

  createRelation: (data: CreateKnowledgeRelationRequest) =>
    client.post<{ relationId: string }>('/knowledge/relations', data),

  getRelationsByTarget: (targetType: KnowledgeRelationType, targetEntityId: string) =>
    client.get<{ documents: KnowledgeDocumentRelationItem[]; notes: OperationalNoteRelationItem[] }>(
      `/knowledge/relations/by-target/${targetType}/${targetEntityId}`
    ),

  getRelationsBySource: (sourceEntityId: string) =>
    client.get<{ items: KnowledgeRelationDto[] }>(
      `/knowledge/relations/by-source/${sourceEntityId}`
    ),
};
