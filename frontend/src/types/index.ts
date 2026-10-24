export interface User {
  id: string;
  email: string;
  fullName: string;
  role: 'Viewer' | 'Owner' | 'Administrator';
  isActive: boolean;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface Document {
  id: string;
  title: string;
  description: string;
  category: string;
  originalFileName: string;
  mimeType: string;
  fileSizeBytes: number;
  ownerId: string;
  ownerName: string;
  ownerEmail: string;
  createdAt: string;
  updatedAt: string;
}

export interface DocumentList {
  documents: Document[];
  total: number;
  page: number;
  pageSize: number;
}

export interface Permission {
  id: string;
  documentId: string;
  userId: string;
  userEmail: string;
  userFullName: string;
  permission: 'Read' | 'Edit';
  grantedById: string;
  grantedByEmail: string;
  grantedAt: string;
}

export interface AuditLog {
  id: string;
  documentId?: string;
  documentTitle?: string;
  userId?: string;
  userEmail?: string;
  action: string;
  isSuccess: boolean;
  ipAddress: string;
  details: string;
  timestamp: string;
}

export interface AuditLogList {
  logs: AuditLog[];
  total: number;
  page: number;
  pageSize: number;
}
