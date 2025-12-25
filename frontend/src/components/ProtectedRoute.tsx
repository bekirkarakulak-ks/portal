import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import type { ReactNode } from 'react';

interface ProtectedRouteProps {
  children: ReactNode;
  requiredPermission?: string;
  requiredPermissions?: string[];
  requireAll?: boolean; // true = all permissions required, false = any permission
}

export const ProtectedRoute = ({
  children,
  requiredPermission,
  requiredPermissions,
  requireAll = false,
}: ProtectedRouteProps) => {
  const location = useLocation();
  const { isAuthenticated, hasPermission, hasAnyPermission } = useAuthStore();

  // Check authentication
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // Check single permission
  if (requiredPermission && !hasPermission(requiredPermission)) {
    return <Navigate to="/unauthorized" replace />;
  }

  // Check multiple permissions
  if (requiredPermissions && requiredPermissions.length > 0) {
    if (requireAll) {
      const hasAll = requiredPermissions.every((p) => hasPermission(p));
      if (!hasAll) {
        return <Navigate to="/unauthorized" replace />;
      }
    } else {
      if (!hasAnyPermission(requiredPermissions)) {
        return <Navigate to="/unauthorized" replace />;
      }
    }
  }

  return <>{children}</>;
};

export default ProtectedRoute;
