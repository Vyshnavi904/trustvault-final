import { api } from './client';
import type { AuthResponse } from '../types';

export const authApi = {
  register: (email: string, password: string, fullName: string, role: string) =>
    api.post<AuthResponse>('/auth/register', { email, password, fullName, role }),

  login: (email: string, password: string) =>
    api.post<AuthResponse>('/auth/login', { email, password }),

  logout: (refreshToken: string) =>
    api.post('/auth/logout', { refreshToken }),

  me: () => api.get('/auth/me'),
};
