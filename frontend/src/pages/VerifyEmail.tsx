import { useState, useEffect } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { authApi } from '../services/api';
import { useAuthStore } from '../store/authStore';
import logo from '../assets/logo.svg';

export const VerifyEmail = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [message, setMessage] = useState('');
  const setUser = useAuthStore((state) => state.setUser);
  const setTokens = useAuthStore((state) => state.setTokens);

  useEffect(() => {
    const verifyToken = async () => {
      const token = searchParams.get('token');

      if (!token) {
        setStatus('error');
        setMessage('Doğrulama linki geçersiz.');
        return;
      }

      try {
        const result = await authApi.verifyEmail(token);

        if (result.success && result.authData) {
          setStatus('success');
          setMessage(result.message);

          // Kullaniciyi otomatik giris yaptir
          setUser({
            userId: result.authData.userId,
            username: result.authData.username,
            email: result.authData.email,
            firstName: result.authData.firstName,
            lastName: result.authData.lastName,
            permissions: result.authData.permissions,
            roles: result.authData.roles,
            modules: [],
          });
          setTokens(result.authData.accessToken, result.authData.refreshToken);

          // 3 saniye sonra dashboard'a yonlendir
          setTimeout(() => {
            navigate('/');
          }, 3000);
        } else {
          setStatus('error');
          setMessage(result.message);
        }
      } catch (err: unknown) {
        const error = err as { response?: { data?: { message?: string } } };
        setStatus('error');
        setMessage(error.response?.data?.message || 'Doğrulama sırasında bir hata oluştu.');
      }
    };

    verifyToken();
  }, [searchParams, navigate, setUser, setTokens]);

  return (
    <div className="login-container">
      <div className="login-card">
        <img src={logo} alt="Logo" className="login-logo" />

        {status === 'loading' && (
          <div className="verify-status">
            <div className="spinner"></div>
            <h2>Email Doğrulanıyor...</h2>
            <p>Lütfen bekleyin...</p>
          </div>
        )}

        {status === 'success' && (
          <div className="verify-status success">
            <div className="success-icon large">&#x2713;</div>
            <h2>Email Doğrulandı!</h2>
            <p className="success-message">{message}</p>
            <p className="info-text">Birazdan ana sayfaya yönlendirileceksiniz...</p>
            <Link to="/" className="btn-primary">
              Ana Sayfaya Git
            </Link>
          </div>
        )}

        {status === 'error' && (
          <div className="verify-status error">
            <div className="error-icon large">&#x2717;</div>
            <h2>Doğrulama Başarısız</h2>
            <p className="error-message">{message}</p>
            <div className="action-buttons">
              <Link to="/login" className="btn-primary">
                Giriş Sayfasına Git
              </Link>
              <Link to="/register" className="btn-secondary">
                Yeni Kayıt Ol
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default VerifyEmail;
