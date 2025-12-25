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
