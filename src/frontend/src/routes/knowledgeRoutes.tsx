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
    </>
  );
}
