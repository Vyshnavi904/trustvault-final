import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { documentsApi } from '../api/documents';
import { Layout } from '../components/Layout';
import { useAuth } from '../contexts/AuthContext';
import { FileText, Download, Search, Upload, Calendar, User } from 'lucide-react';

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function Documents() {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [page, setPage] = useState(1);
  const { user } = useAuth();

  const { data, isLoading, error } = useQuery({
    queryKey: ['documents', search, category, page],
    queryFn: () => documentsApi.search({ q: search || undefined, category: category || undefined, page }),
  });

  const handleDownload = async (id: string, filename: string) => {
    try {
      await documentsApi.download(id, filename);
    } catch {
      alert('Download failed. You may not have permission.');
    }
  };

  return (
    <Layout>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-bold text-slate-900">Documents</h1>
          {(user?.role === 'Owner' || user?.role === 'Administrator') && (
            <Link
              to="/documents/upload"
              className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors text-sm font-medium"
            >
              <Upload className="w-4 h-4" />
              Upload Document
            </Link>
          )}
        </div>

        <div className="flex gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
            <input
              type="text"
              value={search}
              onChange={e => { setSearch(e.target.value); setPage(1); }}
              placeholder="Search documents..."
              className="w-full pl-10 pr-4 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
            />
          </div>
          <input
            type="text"
            value={category}
            onChange={e => { setCategory(e.target.value); setPage(1); }}
            placeholder="Filter by category"
            className="px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm w-48"
          />
        </div>

        {isLoading && <p className="text-slate-500 text-sm">Loading...</p>}

        {error && (
          <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
            Failed to load documents.
          </div>
        )}

        {data && data.data.documents.length === 0 && (
          <div className="text-center py-16 text-slate-400">
            <FileText className="w-12 h-12 mx-auto mb-3 opacity-40" />
            <p className="font-medium">No documents found</p>
            <p className="text-sm">
              {user?.role === 'Viewer'
                ? 'Documents shared with you will appear here.'
                : 'Upload your first document to get started.'}
            </p>
          </div>
        )}

        <div className="space-y-3">
          {data?.data.documents.map(doc => (
            <div
              key={doc.id}
              className="bg-white rounded-xl border border-slate-200 p-4 shadow-sm hover:shadow-md transition-shadow"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1 min-w-0">
                  <Link
                    to={`/documents/${doc.id}`}
                    className="font-semibold text-slate-900 hover:text-blue-600 transition-colors truncate block"
                  >
                    {doc.title}
                  </Link>
                  {doc.description && (
                    <p className="text-sm text-slate-500 mt-0.5 line-clamp-1">{doc.description}</p>
                  )}
                  <div className="flex items-center gap-4 mt-2 text-xs text-slate-400">
                    {doc.category && (
                      <span className="px-2 py-0.5 bg-blue-50 text-blue-700 rounded-full font-medium">
                        {doc.category}
                      </span>
                    )}
                    <span className="flex items-center gap-1">
                      <User className="w-3 h-3" />
                      {doc.ownerName}
                    </span>
                    <span className="flex items-center gap-1">
                      <Calendar className="w-3 h-3" />
                      {new Date(doc.createdAt).toLocaleDateString()}
                    </span>
                    <span>{formatBytes(doc.fileSizeBytes)}</span>
                  </div>
                </div>

                <button
                  onClick={() => handleDownload(doc.id, doc.originalFileName)}
                  className="ml-3 p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                  title="Download"
                >
                  <Download className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>

        {data && data.data.total > (data.data.pageSize || 20) && (
          <div className="flex justify-center gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-3 py-1 border rounded-lg text-sm disabled:opacity-40 hover:bg-slate-50"
            >
              Previous
            </button>
            <span className="px-3 py-1 text-sm text-slate-600">Page {page}</span>
            <button
              onClick={() => setPage(p => p + 1)}
              disabled={data.data.documents.length < (data.data.pageSize || 20)}
              className="px-3 py-1 border rounded-lg text-sm disabled:opacity-40 hover:bg-slate-50"
            >
              Next
            </button>
          </div>
        )}
      </div>
    </Layout>
  );
}
