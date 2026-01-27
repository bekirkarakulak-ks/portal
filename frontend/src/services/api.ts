import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '../store/authStore';

const API_URL = import.meta.env.VITE_API_URL || 'https://portal-api-518226731997.europe-west2.run.app/api';

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - add token to headers
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = useAuthStore.getState().accessToken;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const refreshToken = useAuthStore.getState().refreshToken;
      if (refreshToken) {
        try {
          const response = await axios.post(`${API_URL}/auth/refresh-token`, {
            refreshToken,
          });

          const { accessToken, refreshToken: newRefreshToken } = response.data;
          useAuthStore.getState().setTokens(accessToken, newRefreshToken);

          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          return api(originalRequest);
        } catch (refreshError) {
          useAuthStore.getState().logout();
          window.location.href = '/login';
          return Promise.reject(refreshError);
        }
      }
    }

    return Promise.reject(error);
  }
);

// Types
export interface EmailLookupResponse {
  found: boolean;
  firstName: string | null;
  lastName: string | null;
  department: string | null;
  position: string | null;
  title: string | null;
  phone: string | null;
}

export interface RegisterResponse {
  success: boolean;
  message: string;
  requiresEmailVerification: boolean;
  username: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
}

export interface VerifyEmailResponse {
  success: boolean;
  message: string;
  authData: AuthResponse | null;
}

export interface AuthResponse {
  userId: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiration: string;
  permissions: string[];
  roles: string[];
}

// Auth API functions
export const authApi = {
  checkEmail: async (email: string): Promise<EmailLookupResponse> => {
    const response = await api.get<EmailLookupResponse>('/auth/check-email', {
      params: { email },
    });
    return response.data;
  },

  register: async (data: {
    username: string;
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }): Promise<RegisterResponse> => {
    const response = await api.post<RegisterResponse>('/auth/register', data);
    return response.data;
  },

  verifyEmail: async (token: string): Promise<VerifyEmailResponse> => {
    const response = await api.post<VerifyEmailResponse>('/auth/verify-email', { token });
    return response.data;
  },

  resendVerification: async (email: string): Promise<{ message: string }> => {
    const response = await api.post<{ message: string }>('/auth/resend-verification', { email });
    return response.data;
  },
};

export default api;
