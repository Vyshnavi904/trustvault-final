import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { adminApi } from '../api/admin';
import { Layout } from '../components/Layout';
import { BarChart2, CheckCircle, XCircle } from 'lucide-react';

const actionColors: Record<string, string> = {
  AccessDenied: 'text-red-600',
  DocumentDownloaded: 'text-blue-600',
  DocumentUploaded: 'text-green-600',
  DocumentDeleted: 'text-orange-600',
  PermissionGranted: 'text-purple-600',
  PermissionRevoked: 'text-yellow-600',
  Login: 'text-slate-500',
  Logout: 'text-slate-400',
};

export function AdminAudit() {
  const [page, setPage] = useState(1);
  const [actionFilter, setActionFilter] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['audit-logs', page, actionFilter],
    queryFn: () => adminApi.getAuditLogs({
      action: actionFilter || undefined,
      page,
      pageSize: 50,
    }),
  });

  const logs = data?.data.logs || [];

  return (
    <Layout>
      <div className="space-y-6">
        <div className="flex items-center gap-2">
          <BarChart2 className="w-5 h-5 text-blue-600" />
          <h1 className="text-2xl font-bold text-slate-900">Audit Log</h1>
          {data && (
            <span className="text-sm text-slate-400 ml-2">({data.data.total} total entries)</span>
          )}
        </div>

        <div className="flex gap-3">
          <select
            value={actionFilter}
            onChange={e => { setActionFilter(e.target.value); setPage(1); }}
            className="px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
          >
            <option value="">All Actions</option>
            <option value="DocumentDownloaded">Download</option>
            <option value="DocumentUploaded">Upload</option>
            <option value="DocumentDeleted">Delete</option>
            <option value="AccessDenied">Access Denied</option>
            <option value="PermissionGranted">Permission Granted</option>
            <option value="PermissionRevoked">Permission Revoked</option>
            <option value="Login">Login</option>
          </select>
        </div>

        {isLoading && <p className="text-slate-500 text-sm">Loading...</p>}

        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-slate-600">Time</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600">Action</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600">User</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600">Document</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600">IP</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600">Status</th>
                <th className="text-left px-4 py-3 font-medium text-slate-600">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {logs.map(log => (
                <tr key={log.id} className={`hover:bg-slate-50 ${!log.isSuccess ? 'bg-red-50/40' : ''}`}>
                  <td className="px-4 py-2.5 text-xs text-slate-400 whitespace-nowrap">
                    {new Date(log.timestamp).toLocaleString()}
                  </td>
                  <td className="px-4 py-2.5">
                    <span className={`font-medium text-xs ${actionColors[log.action] || 'text-slate-600'}`}>
                      {log.action.replace(/([A-Z])/g, ' $1').trim()}
                    </span>
                  </td>
                  <td className="px-4 py-2.5 text-xs text-slate-600">{log.userEmail || '—'}</td>
                  <td className="px-4 py-2.5 text-xs text-slate-600 max-w-32 truncate">
                    {log.documentTitle || '—'}
                  </td>
                  <td className="px-4 py-2.5 text-xs text-slate-400">{log.ipAddress || '—'}</td>
                  <td className="px-4 py-2.5">
                    {log.isSuccess
                      ? <CheckCircle className="w-4 h-4 text-green-500" />
                      : <XCircle className="w-4 h-4 text-red-500" />
                    }
                  </td>
                  <td className="px-4 py-2.5 text-xs text-slate-400 max-w-48 truncate">{log.details}</td>
                </tr>
              ))}
              {logs.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-slate-400 text-sm">
                    No audit entries found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

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
            disabled={logs.length < 50}
            className="px-3 py-1 border rounded-lg text-sm disabled:opacity-40 hover:bg-slate-50"
          >
            Next
          </button>
        </div>
      </div>
    </Layout>
  );
}
