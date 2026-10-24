import { api } from './client';
import type { Permission } from '../types';

export const permissionsApi = {
  getByDocument: (documentId: string) =>
    api.get<Permission[]>(`/documents/${documentId}/permissions`),

  grant: (documentId: string, userId: string, permission: 'Read' | 'Edit') =>
    api.post<Permission>(`/documents/${documentId}/permissions`, { userId, permission }),

  revoke: (documentId: string, permissionId: string) =>
    api.delete(`/documents/${documentId}/permissions/${permissionId}`),
};
