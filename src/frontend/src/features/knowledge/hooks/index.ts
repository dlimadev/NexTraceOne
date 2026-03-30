import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { knowledgeApi } from '../api/knowledge';
import type {
  DocumentCategory,
  DocumentStatus,
  NoteSeverity,
  KnowledgeRelationType,
  CreateKnowledgeDocumentRequest,
  CreateOperationalNoteRequest,
  CreateKnowledgeRelationRequest,
} from '../../../types';

export const knowledgeQueryKeys = {
  all: ['knowledge'] as const,
  documents: () => [...knowledgeQueryKeys.all, 'documents'] as const,
  documentList: (filters?: Record<string, unknown>) =>
    [...knowledgeQueryKeys.documents(), 'list', filters] as const,
  document: (id: string) => [...knowledgeQueryKeys.documents(), 'detail', id] as const,
  notes: () => [...knowledgeQueryKeys.all, 'notes'] as const,
  noteList: (filters?: Record<string, unknown>) =>
    [...knowledgeQueryKeys.notes(), 'list', filters] as const,
  search: (q: string, scope?: string) =>
    [...knowledgeQueryKeys.all, 'search', q, scope] as const,
  relationsByTarget: (targetType: string, targetEntityId: string) =>
    [...knowledgeQueryKeys.all, 'relations', 'target', targetType, targetEntityId] as const,
  relationsBySource: (sourceEntityId: string) =>
    [...knowledgeQueryKeys.all, 'relations', 'source', sourceEntityId] as const,
};

export function useKnowledgeDocuments(params: {
  category?: DocumentCategory;
  status?: DocumentStatus;
  page?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: knowledgeQueryKeys.documentList(params),
    queryFn: () => knowledgeApi.listDocuments(params).then((r) => r.data),
    staleTime: 30_000,
  });
}

export function useKnowledgeDocument(documentId: string | undefined) {
  return useQuery({
    queryKey: knowledgeQueryKeys.document(documentId ?? ''),
    queryFn: () => knowledgeApi.getDocumentById(documentId!).then((r) => r.data),
    enabled: !!documentId,
    staleTime: 30_000,
  });
}

export function useOperationalNotes(params: {
  severity?: NoteSeverity;
  contextType?: string;
  contextEntityId?: string;
  isResolved?: boolean;
  page?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: knowledgeQueryKeys.noteList(params),
    queryFn: () => knowledgeApi.listOperationalNotes(params).then((r) => r.data),
    staleTime: 30_000,
  });
}

export function useKnowledgeSearch(q: string, scope?: string) {
  return useQuery({
    queryKey: knowledgeQueryKeys.search(q, scope),
    queryFn: () => knowledgeApi.search({ q, scope, maxResults: 20 }).then((r) => r.data),
    enabled: q.trim().length >= 2,
    staleTime: 15_000,
  });
}

export function useKnowledgeRelationsByTarget(
  targetType: KnowledgeRelationType,
  targetEntityId: string | undefined
) {
  return useQuery({
    queryKey: knowledgeQueryKeys.relationsByTarget(targetType, targetEntityId ?? ''),
    queryFn: () =>
      knowledgeApi.getRelationsByTarget(targetType, targetEntityId!).then((r) => r.data),
    enabled: !!targetEntityId,
    staleTime: 30_000,
  });
}

export function useKnowledgeRelationsBySource(sourceEntityId: string | undefined) {
  return useQuery({
    queryKey: knowledgeQueryKeys.relationsBySource(sourceEntityId ?? ''),
    queryFn: () =>
      knowledgeApi.getRelationsBySource(sourceEntityId!).then((r) => r.data),
    enabled: !!sourceEntityId,
    staleTime: 30_000,
  });
}

export function useCreateKnowledgeDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateKnowledgeDocumentRequest) =>
      knowledgeApi.createDocument(data).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: knowledgeQueryKeys.documents() });
    },
  });
}

export function useCreateOperationalNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateOperationalNoteRequest) =>
      knowledgeApi.createOperationalNote(data).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: knowledgeQueryKeys.notes() });
    },
  });
}

export function useCreateKnowledgeRelation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateKnowledgeRelationRequest) =>
      knowledgeApi.createRelation(data).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: knowledgeQueryKeys.all });
    },
  });
}
