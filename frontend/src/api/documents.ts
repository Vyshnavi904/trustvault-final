import { api } from './client';
import type { Document, DocumentList } from '../types';

export interface SearchParams {
  q?: string;
  category?: string;
  ownerId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export const documentsApi = {
  search: (params: SearchParams = {}) =>
    api.get<DocumentList>('/documents', { params }),

  getById: (id: string) =>
    api.get<Document>(`/documents/${id}`),

  upload: (data: FormData) =>
    api.post<Document>('/documents', data, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }),

  update: (id: string, data: FormData) =>
    api.put<Document>(`/documents/${id}`, data, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }),

  delete: (id: string) =>
    api.delete(`/documents/${id}`),

  downloadUrl: (id: string) => {
    const token = localStorage.getItem('accessToken');
    const base = import.meta.env.VITE_API_URL || 'http://localhost:5260';
    return `${base}/api/documents/${id}/download?access_token=${token}`;
  },

  download: async (id: string, filename: string) => {
    const token = localStorage.getItem('accessToken');
    const base = import.meta.env.VITE_API_URL || 'http://localhost:5260';
    const response = await fetch(`${base}/api/documents/${id}/download`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) throw new Error('Download failed');
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  },
};
