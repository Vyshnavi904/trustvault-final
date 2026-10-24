import { useState, type FormEvent, useRef } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { documentsApi } from '../api/documents';
import { Layout } from '../components/Layout';
import { Upload, File, X, ArrowLeft, Save } from 'lucide-react';

const ALLOWED_TYPES = [
  'application/pdf', 'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'text/plain', 'text/csv', 'image/png', 'image/jpeg', 'application/zip',
];
const MAX_SIZE = 50 * 1024 * 1024;

export function EditDocument() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const fileRef = useRef<HTMLInputElement>(null);

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('');
  const [newFile, setNewFile] = useState<File | null>(null);
  const [error, setError] = useState('');
  const [initialized, setInitialized] = useState(false);

  const { isLoading, isError } = useQuery({
    queryKey: ['document', id],
    queryFn: () => documentsApi.getById(id!),
    enabled: !initialized,
    select: (res) => res.data,
    // Once fetched, prefill the form
  });

  // Prefill form once document loads — separate from query to avoid re-running
  const { data: docRes } = useQuery({
    queryKey: ['document', id],
    queryFn: () => documentsApi.getById(id!),
    staleTime: Infinity,
  });

  if (!initialized && docRes?.data) {
    setTitle(docRes.data.title);
    setDescription(docRes.data.description);
    setCategory(docRes.data.category);
    setInitialized(true);
  }

  const updateMutation = useMutation({
    mutationFn: (fd: FormData) => documentsApi.update(id!, fd),
    onSuccess: (res) => navigate(`/documents/${res.data.id}`),
    onError: (err: any) =>
      setError(err.response?.data?.error || 'Update failed. Check your permissions.'),
  });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (!f) return;
    if (!ALLOWED_TYPES.includes(f.type)) {
      setError(`File type "${f.type}" is not allowed.`);
      return;
    }
    if (f.size > MAX_SIZE) {
      setError('File must be smaller than 50 MB.');
      return;
    }
    setError('');
    setNewFile(f);
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');

    const fd = new FormData();
    fd.append('title', title);
    fd.append('description', description);
    fd.append('category', category);
    if (newFile) fd.append('file', newFile);

    updateMutation.mutate(fd);
  };

  if (isLoading && !initialized) {
    return <Layout><p className="text-slate-500">Loading document...</p></Layout>;
  }

  if (isError || (!isLoading && !docRes?.data)) {
    return (
      <Layout>
        <p className="text-red-500">Document not found or you do not have edit permission.</p>
      </Layout>
    );
  }

  const doc = docRes?.data;

  return (
    <Layout>
      <div className="max-w-2xl space-y-6">
        <Link
          to={`/documents/${id}`}
          className="flex items-center gap-1 text-sm text-slate-500 hover:text-slate-900"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Document
        </Link>

        <h1 className="text-2xl font-bold text-slate-900">Edit Document</h1>

        <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Current file info */}
            {doc && (
              <div className="p-3 bg-slate-50 rounded-lg border border-slate-200">
                <p className="text-xs text-slate-400 mb-1 uppercase tracking-wider">Current File</p>
                <p className="text-sm font-medium text-slate-700">{doc.originalFileName}</p>
                <p className="text-xs text-slate-400 mt-0.5">{doc.mimeType}</p>
              </div>
            )}

            {/* Replace file (optional) */}
            <div>
              <p className="text-sm font-medium text-slate-700 mb-2">
                Replace File <span className="text-slate-400 font-normal">(optional — leave empty to keep current file)</span>
              </p>
              <div
                onClick={() => fileRef.current?.click()}
                className={`border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-colors ${
                  newFile
                    ? 'border-blue-400 bg-blue-50'
                    : 'border-slate-300 hover:border-blue-400 hover:bg-blue-50'
                }`}
              >
                {newFile ? (
                  <div className="flex flex-col items-center gap-2">
                    <File className="w-8 h-8 text-blue-500" />
                    <p className="font-medium text-slate-700 text-sm">{newFile.name}</p>
                    <p className="text-xs text-slate-400">{(newFile.size / 1024 / 1024).toFixed(2)} MB</p>
                    <button
                      type="button"
                      onClick={e => {
                        e.stopPropagation();
                        setNewFile(null);
                        if (fileRef.current) fileRef.current.value = '';
                      }}
                      className="text-xs text-red-500 hover:underline flex items-center gap-1"
                    >
                      <X className="w-3 h-3" /> Remove new file
                    </button>
                  </div>
                ) : (
                  <div className="flex flex-col items-center gap-2">
                    <Upload className="w-8 h-8 text-slate-400" />
                    <p className="text-sm text-slate-500">Click to choose a replacement file</p>
                    <p className="text-xs text-slate-400">PDF, Word, Excel, CSV, Images, ZIP — max 50 MB</p>
                  </div>
                )}
                <input
                  ref={fileRef}
                  type="file"
                  className="hidden"
                  onChange={handleFileChange}
                  accept={ALLOWED_TYPES.join(',')}
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Title *</label>
              <input
                type="text"
                value={title}
                onChange={e => setTitle(e.target.value)}
                required
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
              <textarea
                value={description}
                onChange={e => setDescription(e.target.value)}
                rows={3}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm resize-none"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Category</label>
              <input
                type="text"
                value={category}
                onChange={e => setCategory(e.target.value)}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                placeholder="e.g. Finance, HR, Legal"
              />
            </div>

            <div className="flex gap-3 pt-2">
              <Link
                to={`/documents/${id}`}
                className="px-4 py-2 border border-slate-300 rounded-lg text-sm hover:bg-slate-50 text-center"
              >
                Cancel
              </Link>
              <button
                type="submit"
                disabled={updateMutation.isPending}
                className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50"
              >
                {updateMutation.isPending
                  ? 'Saving...'
                  : <><Save className="w-4 h-4" /> Save Changes</>
                }
              </button>
            </div>
          </form>
        </div>
      </div>
    </Layout>
  );
}
