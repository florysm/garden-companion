import { apiClient } from './client'

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface AuthResponse {
  userId: string
  householdId: string | null
  email: string
  displayName: string
  accessToken: string
  refreshToken: string
  accessTokenExpiry: string
}

export interface RefreshResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiry: string
}

export const authApi = {
  register: (body: RegisterRequest) =>
    apiClient.post<AuthResponse>('/api/auth/register', body),

  login: (body: LoginRequest) =>
    apiClient.post<AuthResponse>('/api/auth/login', body),

  refresh: (token: string) =>
    apiClient.post<RefreshResponse>('/api/auth/refresh', { token }),

  forgotPassword: (email: string) =>
    apiClient.post('/api/auth/forgot-password', { email }),

  resetPassword: (token: string, newPassword: string) =>
    apiClient.post('/api/auth/reset-password', { token, newPassword }),
}
