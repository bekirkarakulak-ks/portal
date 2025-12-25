import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAuthStore } from './store/authStore';

// Pages
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Bordro from './pages/Bordro';
import Izinler from './pages/Izinler';
import Butce from './pages/Butce';
import Unauthorized from './pages/Unauthorized';

// Components
import ProtectedRoute from './components/ProtectedRoute';

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

      {/* Protected Routes */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />

      {/* IK Routes - Kendi bilgileri */}
      <Route
        path="/ik/bordro"
        element={
          <ProtectedRoute requiredPermission="IK.Bordro.KendiGoruntule">
            <Bordro />
          </ProtectedRoute>
        }
      />
      <Route
        path="/ik/izinler"
        element={
          <ProtectedRoute requiredPermission="IK.Izin.KendiGoruntule">
            <Izinler />
          </ProtectedRoute>
        }
      />

      {/* Butce Routes */}
      <Route
        path="/butce"
        element={
          <ProtectedRoute requiredPermission="BUTCE.Kendi.Goruntule">
            <Butce />
          </ProtectedRoute>
        }
      />

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
