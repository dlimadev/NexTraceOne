import { lazy } from 'react';
import { Route } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

const KnowledgeHubPage = lazy(() =>
  import('../features/knowledge').then(m => ({ default: m.KnowledgeHubPage }))
);
const KnowledgeDocumentPage = lazy(() =>
  import('../features/knowledge').then(m => ({ default: m.KnowledgeDocumentPage }))
);
const OperationalNotesPage = lazy(() =>
  import('../features/knowledge').then(m => ({ default: m.OperationalNotesPage }))
);
const KnowledgeGraphPage = lazy(() =>
  import('../features/knowledge/pages/KnowledgeGraphPage').then(m => ({ default: m.KnowledgeGraphPage }))
);
const AutoDocumentationPage = lazy(() =>
  import('../features/knowledge/pages/AutoDocumentationPage').then(m => ({ default: m.AutoDocumentationPage }))
);

export function KnowledgeRoutes() {
  return (
    <>
      <Route
        path="/knowledge"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <KnowledgeHubPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/knowledge/documents/:documentId"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <KnowledgeDocumentPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/knowledge/notes"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <OperationalNotesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/knowledge/graph"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <KnowledgeGraphPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/knowledge/auto-documentation"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <AutoDocumentationPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/knowledge/auto-documentation/:serviceName"
        element={
          <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
            <AutoDocumentationPage />
          </ProtectedRoute>
        }
      />
    </>
  );
}
