import { useState, useEffect, useRef } from 'react';
import client from '../../../api/client';

/** Mirrors the backend PreviewSchemaElement record. */
export interface PreviewSchemaElement {
  name: string;
  dataType: string;
  isRequired: boolean;
  description?: string | null;
  format?: string | null;
  defaultValue?: string | null;
  isDeprecated: boolean;
  children?: PreviewSchemaElement[] | null;
}

/** Mirrors the backend PreviewOperation record. */
export interface PreviewOperation {
  operationId: string;
  name: string;
  description?: string | null;
  method: string;
  path: string;
  isDeprecated: boolean;
  tags: string[];
  inputParameters: PreviewSchemaElement[];
  outputFields: PreviewSchemaElement[];
}

/** Mirrors the backend PreviewModel record. */
export interface PreviewModel {
  protocol: string;
  title: string;
  specVersion: string;
  description?: string | null;
  servers: string[];
  tags: string[];
  securitySchemes: string[];
  operations: PreviewOperation[];
  schemas: PreviewSchemaElement[];
  operationCount: number;
  schemaCount: number;
  hasSecurityDefinitions: boolean;
  hasExamples: boolean;
  hasDescriptions: boolean;
}

/** Backend response for parse-preview. */
export interface ParsePreviewResponse {
  isValid: boolean;
  errorMessage?: string | null;
  preview?: PreviewModel | null;
}

/**
 * Hook que parseia conteúdo de spec ad-hoc via backend e retorna o modelo
 * canónico para o live preview do Contract Studio Editor.
 * Debounce integrado para evitar chamadas excessivas ao backend durante edição.
 */
export function useSpecPreview(specContent: string, protocol: string, debounceMs = 500) {
  const [preview, setPreview] = useState<PreviewModel | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (!specContent.trim()) {
      setPreview(null);
      setError(null);
      return;
    }

    const timer = setTimeout(async () => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setIsLoading(true);
      try {
        const { data } = await client.post<ParsePreviewResponse>(
          '/contracts/parse-preview',
          { specContent, protocol },
          { signal: controller.signal },
        );

        if (!controller.signal.aborted) {
          if (data.isValid && data.preview) {
            setPreview(data.preview);
            setError(null);
          } else {
            setPreview(null);
            setError(data.errorMessage ?? 'Failed to parse specification');
          }
        }
      } catch (err: unknown) {
        if (!controller.signal.aborted) {
          setPreview(null);
          setError(err instanceof Error ? err.message : 'Parse error');
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }, debounceMs);

    return () => {
      clearTimeout(timer);
      abortRef.current?.abort();
    };
  }, [specContent, protocol, debounceMs]);

  return { preview, error, isLoading };
}
