export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
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

export interface UserInfo {
  userId: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  permissions: string[];
  roles: string[];
  modules: ModuleInfo[];
}

export interface ModuleInfo {
  code: string;
  name: string;
  icon: string | null;
  permissions: string[];
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

// Şifre değiştirme
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ChangePasswordResponse {
  success: boolean;
  message: string;
}

// Şifremi unuttum
export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  success: boolean;
  message: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface ResetPasswordResponse {
  success: boolean;
  message: string;
}
