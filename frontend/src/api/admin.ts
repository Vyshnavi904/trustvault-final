import { api } from './client';
import type { User, Document, AuditLogList } from '../types';

export const adminApi = {
  getUsers: () => api.get<User[]>('/admin/users'),

  setActive: (userId: string, isActive: boolean) =>
    api.patch<User>(`/admin/users/${userId}/active`, { isActive }),

  changeRole: (userId: string, role: string) =>
    api.patch<User>(`/admin/users/${userId}/role`, { role }),

  getAllDocuments: () => api.get<Document[]>('/admin/documents'),

  getAuditLogs: (params?: {
    documentId?: string;
    userId?: string;
    action?: string;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }) => api.get<AuditLogList>('/audit', { params }),
};
