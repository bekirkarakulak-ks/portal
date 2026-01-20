import { useState, useEffect } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import authService from '../services/authService';
import logo from '../assets/logo.svg';

export default function ResetPassword() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    if (!token) {
      setError('Gecersiz sifre sifirlama linki.');
    }
  }, [token]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // Validasyonlar
    if (newPassword.length < 6) {
      setError('Sifre en az 6 karakter olmalidir.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setError('Sifreler eslesmiyor.');
      return;
    }

    if (!token) {
      setError('Gecersiz sifre sifirlama linki.');
      return;
    }

    setIsLoading(true);

    try {
      const response = await authService.resetPassword({ token, newPassword });
      if (response.success) {
        setSuccess(true);
        // 3 saniye sonra login sayfasina yonlendir
        setTimeout(() => {
          navigate('/login');
        }, 3000);
      } else {
        setError(response.message);
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Bir hata olustu. Lutfen tekrar deneyin.');
    } finally {
      setIsLoading(false);
    }
  };

  if (success) {
    return (
      <div className="login-container">
        <div className="login-card">
          <img src={logo} alt="Logo" className="login-logo" />
          <div className="success-container">
            <div className="success-icon large">&#10003;</div>
            <h2>Sifre Sifirlandi</h2>
            <p className="info-text">
              Sifreniz basariyla degistirildi. Simdi yeni sifrenizle giris yapabilirsiniz.
            </p>
            <p className="info-text small">
              Giris sayfasina yonlendiriliyorsunuz...
            </p>
            <Link to="/login" className="btn-primary">
              Giris Yap
            </Link>
          </div>
        </div>
      </div>
    );
  }

  if (!token) {
    return (
      <div className="login-container">
        <div className="login-card">
          <img src={logo} alt="Logo" className="login-logo" />
          <div className="verify-status error">
            <div className="error-icon large">!</div>
            <h2>Gecersiz Link</h2>
            <p className="info-text">
              Bu sifre sifirlama linki gecersiz veya suresi dolmus.
            </p>
            <Link to="/forgot-password" className="btn-primary">
              Yeni Link Talep Et
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="login-container">
      <div className="login-card">
        <img src={logo} alt="Logo" className="login-logo" />
        <h1>Yeni Sifre Belirle</h1>
        <p className="info-text" style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
          Lutfen yeni sifrenizi girin.
        </p>

        {error && <div className="error-message">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="newPassword">Yeni Sifre</label>
            <input
              type="password"
              id="newPassword"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="En az 6 karakter"
              required
              minLength={6}
              disabled={isLoading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword">Sifre Tekrar</label>
            <input
              type="password"
              id="confirmPassword"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="Sifrenizi tekrar girin"
              required
              minLength={6}
              disabled={isLoading}
            />
          </div>

          <button type="submit" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor...' : 'Sifremi Degistir'}
          </button>
        </form>

        <div className="register-link">
          <Link to="/login">Giris sayfasina don</Link>
        </div>
      </div>
    </div>
  );
}
