import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import authService from '../services/authService';
import type { LoginRequest, RegisterRequest } from '../types/auth';

export const useAuth = () => {
  const navigate = useNavigate();
  const {
    user,
    isAuthenticated,
    setTokens,
    setUser,
    logout: storeLogout,
    hasPermission,
    hasAnyPermission,
    hasRole,
    getModules,
    refreshToken,
  } = useAuthStore();

  const login = useCallback(
    async (data: LoginRequest) => {
      const response = await authService.login(data);
      setTokens(response.accessToken, response.refreshToken);

      // Fetch full user info
      const userInfo = await authService.getCurrentUser();
      setUser(userInfo);

      navigate('/');
    },
    [navigate, setTokens, setUser]
  );

  const register = useCallback(
    async (data: RegisterRequest) => {
      const response = await authService.register(data);
      setTokens(response.accessToken, response.refreshToken);

      const userInfo = await authService.getCurrentUser();
      setUser(userInfo);

      navigate('/');
    },
    [navigate, setTokens, setUser]
  );

  const logout = useCallback(async () => {
    if (refreshToken) {
      try {
        await authService.logout(refreshToken);
      } catch {
        // Ignore logout errors
      }
    }
    storeLogout();
    navigate('/login');
  }, [refreshToken, storeLogout, navigate]);

  const refreshUserInfo = useCallback(async () => {
    const userInfo = await authService.getCurrentUser();
    setUser(userInfo);
  }, [setUser]);

  return {
    user,
    isAuthenticated,
    login,
    register,
    logout,
    refreshUserInfo,
    hasPermission,
    hasAnyPermission,
    hasRole,
    getModules,
  };
};

export default useAuth;
