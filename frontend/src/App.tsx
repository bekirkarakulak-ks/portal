import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAuthStore } from './store/authStore';

// Pages
import Login from './pages/Login';
import Register from './pages/Register';
import VerifyEmail from './pages/VerifyEmail';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import Dashboard from './pages/Dashboard';
import Bordro from './pages/Bordro';
import Izinler from './pages/Izinler';
import Butce from './pages/Butce';
import Unauthorized from './pages/Unauthorized';

// Admin Pages
import AdminDashboard from './pages/admin/AdminDashboard';
import UserManagement from './pages/admin/UserManagement';
import RoleManagement from './pages/admin/RoleManagement';
import ModuleManagement from './pages/admin/ModuleManagement';
import OrganizationRules from './pages/admin/OrganizationRules';

// Components
import ProtectedRoute from './components/ProtectedRoute';
import AdminLayout from './components/AdminLayout';

import './App.css';

const queryClient = new QueryClient();

function AppRoutes() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (
    <Routes>
      {/* Public Routes */}
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/" replace /> : <Login />}
      />
      <Route
        path="/register"
        element={isAuthenticated ? <Navigate to="/" replace /> : <Register />}
      />
      <Route path="/unauthorized" element={<Unauthorized />} />
      <Route path="/verify-email" element={<VerifyEmail />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/reset-password" element={<ResetPassword />} />

      {/* Protected Routes */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />

      {/* IK Routes */}
      <Route
        path="/ik/bordro"
        element={
          <ProtectedRoute requiredPermission="IK.Bordro.Kendi">
            <Bordro />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ik/izinler"
        element={
          <ProtectedRoute requiredPermission="IK.Izin.Kendi">
            <Izinler />
          </ProtectedRoute>
        }
      />

      {/* Butce Routes */}
      <Route
        path="/butce"
        element={
          <ProtectedRoute requiredPermission="Butce.Kendi">
            <Butce />
          </ProtectedRoute>
        }
      />

      {/* Admin Routes */}
      <Route
        path="/admin"
        element={
          <ProtectedRoute requiredPermission="Admin.Access">
            <AdminLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<AdminDashboard />} />
        <Route path="users" element={<UserManagement />} />
        <Route path="roles" element={<RoleManagement />} />
        <Route path="modules" element={<ModuleManagement />} />
        <Route path="organization" element={<OrganizationRules />} />
      </Route>

      {/* Catch all */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
