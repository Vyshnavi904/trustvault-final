import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { Documents } from './pages/Documents';
import { DocumentDetail } from './pages/DocumentDetail';
import { UploadDocument } from './pages/UploadDocument';
import { AdminUsers } from './pages/AdminUsers';
import { AdminAudit } from './pages/AdminAudit';
import { EditDocument } from './pages/EditDocument';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />

            <Route path="/documents" element={
              <ProtectedRoute><Documents /></ProtectedRoute>
            } />
            <Route path="/documents/upload" element={
              <ProtectedRoute roles={['Owner', 'Administrator']}><UploadDocument /></ProtectedRoute>
            } />
            <Route path="/documents/:id" element={
              <ProtectedRoute><DocumentDetail /></ProtectedRoute>
            } />
            <Route path="/documents/:id/edit" element={
              <ProtectedRoute><EditDocument /></ProtectedRoute>
            } />

            <Route path="/admin/users" element={
              <ProtectedRoute roles={['Administrator']}><AdminUsers /></ProtectedRoute>
            } />
            <Route path="/admin/audit" element={
              <ProtectedRoute roles={['Administrator']}><AdminAudit /></ProtectedRoute>
            } />

            <Route path="/" element={<Navigate to="/documents" replace />} />
            <Route path="*" element={<Navigate to="/documents" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}

export default App;
