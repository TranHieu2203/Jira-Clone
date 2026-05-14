import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';
import { AppShell } from '@/components/AppShell';
import LoginPage from '@/pages/LoginPage';
import MetadataListPage from '@/pages/MetadataListPage';
import TemplateListPage from '@/pages/TemplateListPage';
import TemplateEditorPage from '@/pages/TemplateEditorPage';
import SubmissionPage from '@/pages/SubmissionPage';

function RequireAuth({ children }: { children: React.ReactNode }) {
  const token = useAuthStore((s) => s.accessToken);
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/templates" replace />} />
        <Route path="metadata" element={<MetadataListPage />} />
        <Route path="templates" element={<TemplateListPage />} />
        <Route path="templates/:id/edit" element={<TemplateEditorPage />} />
        <Route path="templates/:id/submit" element={<SubmissionPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
