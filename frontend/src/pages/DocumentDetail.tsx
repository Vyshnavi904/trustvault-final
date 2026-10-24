import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { documentsApi } from '../api/documents';
import { permissionsApi } from '../api/permissions';
import { adminApi } from '../api/admin';
import { Layout } from '../components/Layout';
import { useAuth } from '../contexts/AuthContext';
import { Download, Edit, Trash2, Share2, X, ArrowLeft } from 'lucide-react';

export function DocumentDetail() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const qc = useQueryClient();

  const [showGrantModal, setShowGrantModal] = useState(false);
  const [grantEmail, setGrantEmail] = useState('');
  const [grantPermission, setGrantPermission] = useState<'Read' | 'Edit'>('Read');
  const [grantError, setGrantError] = useState('');

  const { data: docRes, isLoading } = useQuery({
    queryKey: ['document', id],
    queryFn: () => documentsApi.getById(id!),
  });

  const { data: permsRes } = useQuery({
    queryKey: ['permissions', id],
    queryFn: () => permissionsApi.getByDocument(id!),
    enabled: docRes?.data.ownerId === user?.id || user?.role === 'Administrator',
  });

  const { data: usersRes } = useQuery({
    queryKey: ['admin-users'],
    queryFn: () => adminApi.getUsers(),
    enabled: showGrantModal,
  });

  const deleteMutation = useMutation({
    mutationFn: () => documentsApi.delete(id!),
    onSuccess: () => navigate('/documents'),
  });

  const grantMutation = useMutation({
    mutationFn: (data: { userId: string; permission: 'Read' | 'Edit' }) =>
      permissionsApi.grant(id!, data.userId, data.permission),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['permissions', id] });
      setShowGrantModal(false);
      setGrantEmail('');
    },
    onError: (err: any) => setGrantError(err.response?.data?.error || 'Failed to grant access.'),
  });

  const revokeMutation = useMutation({
    mutationFn: (permId: string) => permissionsApi.revoke(id!, permId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['permissions', id] }),
  });

  const handleGrant = () => {
    setGrantError('');
    const users = usersRes?.data || [];
    const target = users.find(u => u.email.toLowerCase() === grantEmail.toLowerCase());
    if (!target) {
      setGrantError('User not found with that email.');
      return;
    }
    grantMutation.mutate({ userId: target.id, permission: grantPermission });
  };

  const handleDownload = async () => {
    if (!doc) return;
    try {
      await documentsApi.download(doc.id, doc.originalFileName);
    } catch {
      alert('Download failed.');
    }
  };

  if (isLoading) return <Layout><p className="text-slate-500">Loading...</p></Layout>;

  const doc = docRes?.data;
  if (!doc) return <Layout><p className="text-red-500">Document not found or access denied.</p></Layout>;

  const isOwner = doc.ownerId === user?.id || user?.role === 'Administrator';
  const perms = permsRes?.data || [];

  return (
    <Layout>
      <div className="max-w-3xl space-y-6">
        <Link to="/documents" className="flex items-center gap-1 text-sm text-slate-500 hover:text-slate-900">
          <ArrowLeft className="w-4 h-4" />
          Back to Documents
        </Link>

        <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-2xl font-bold text-slate-900">{doc.title}</h1>
              {doc.category && (
                <span className="inline-block mt-1 px-2 py-0.5 bg-blue-50 text-blue-700 rounded-full text-xs font-medium">
                  {doc.category}
                </span>
              )}
            </div>
            <div className="flex items-center gap-2">
              {isOwner && (
                <>
                  <Link
                    to={`/documents/${id}/edit`}
                    className="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                    title="Edit"
                  >
                    <Edit className="w-4 h-4" />
                  </Link>
                  <button
                    onClick={() => setShowGrantModal(true)}
                    className="p-2 text-slate-400 hover:text-green-600 hover:bg-green-50 rounded-lg transition-colors"
                    title="Share"
                  >
                    <Share2 className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => {
                      if (confirm('Delete this document? This cannot be undone.'))
                        deleteMutation.mutate();
                    }}
                    className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    title="Delete"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </>
              )}
              <button
                onClick={handleDownload}
                className="flex items-center gap-1 px-3 py-2 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 transition-colors"
              >
                <Download className="w-4 h-4" />
                Download
              </button>
            </div>
          </div>

          {doc.description && (
            <p className="mt-4 text-slate-600">{doc.description}</p>
          )}

          <div className="mt-6 grid grid-cols-2 gap-4 text-sm">
            <div>
              <p className="text-slate-400 text-xs uppercase tracking-wider mb-1">File</p>
              <p className="text-slate-700 font-medium">{doc.originalFileName}</p>
            </div>
            <div>
              <p className="text-slate-400 text-xs uppercase tracking-wider mb-1">Type</p>
              <p className="text-slate-700">{doc.mimeType}</p>
            </div>
            <div>
              <p className="text-slate-400 text-xs uppercase tracking-wider mb-1">Owner</p>
              <p className="text-slate-700">{doc.ownerName} ({doc.ownerEmail})</p>
            </div>
            <div>
              <p className="text-slate-400 text-xs uppercase tracking-wider mb-1">Uploaded</p>
              <p className="text-slate-700">{new Date(doc.createdAt).toLocaleString()}</p>
            </div>
          </div>
        </div>

        {isOwner && (
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-semibold text-slate-900">Access Permissions</h2>
              <button
                onClick={() => setShowGrantModal(true)}
                className="text-sm text-blue-600 hover:underline"
              >
                + Grant Access
              </button>
            </div>

            {perms.length === 0 ? (
              <p className="text-sm text-slate-400">No one else has access to this document.</p>
            ) : (
              <div className="space-y-2">
                {perms.map(p => (
                  <div key={p.id} className="flex items-center justify-between p-3 bg-slate-50 rounded-lg">
                    <div>
                      <p className="text-sm font-medium text-slate-700">{p.userFullName}</p>
                      <p className="text-xs text-slate-400">{p.userEmail}</p>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                        p.permission === 'Edit'
                          ? 'bg-orange-100 text-orange-700'
                          : 'bg-green-100 text-green-700'
                      }`}>
                        {p.permission}
                      </span>
                      <button
                        onClick={() => revokeMutation.mutate(p.id)}
                        className="text-slate-400 hover:text-red-500 transition-colors"
                        title="Revoke access"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {showGrantModal && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="bg-white rounded-xl shadow-2xl p-6 w-full max-w-md">
              <div className="flex items-center justify-between mb-4">
                <h3 className="font-semibold text-slate-900">Grant Access</h3>
                <button onClick={() => { setShowGrantModal(false); setGrantError(''); }}>
                  <X className="w-4 h-4 text-slate-400 hover:text-slate-700" />
                </button>
              </div>

              {grantError && (
                <div className="mb-3 p-2 bg-red-50 border border-red-200 rounded text-red-700 text-sm">
                  {grantError}
                </div>
              )}

              <div className="space-y-3">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">User Email</label>
                  <input
                    type="email"
                    value={grantEmail}
                    onChange={e => setGrantEmail(e.target.value)}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                    placeholder="user@example.com"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">Permission</label>
                  <select
                    value={grantPermission}
                    onChange={e => setGrantPermission(e.target.value as 'Read' | 'Edit')}
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                  >
                    <option value="Read">Read — can view and download</option>
                    <option value="Edit">Edit — can also modify the document</option>
                  </select>
                </div>
                <div className="flex gap-2 pt-2">
                  <button
                    onClick={() => { setShowGrantModal(false); setGrantError(''); }}
                    className="flex-1 px-3 py-2 border border-slate-300 rounded-lg text-sm hover:bg-slate-50"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleGrant}
                    disabled={grantMutation.isPending}
                    className="flex-1 px-3 py-2 bg-blue-600 text-white rounded-lg text-sm hover:bg-blue-700 disabled:opacity-50"
                  >
                    {grantMutation.isPending ? 'Granting...' : 'Grant Access'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
}
