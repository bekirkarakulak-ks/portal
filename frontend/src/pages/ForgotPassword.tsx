import { useState } from 'react';
import { Link } from 'react-router-dom';
import authService from '../services/authService';
import logo from '../assets/logo.svg';

export default function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      const response = await authService.forgotPassword({ email });
      if (response.success) {
        setSuccess(true);
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
            <h2>Email Gonderildi</h2>
            <p className="info-text">
              Eger bu email adresi sistemde kayitliysa, sifre sifirlama linki gonderildi.
              Lutfen email kutunuzu kontrol edin.
            </p>
            <p className="info-text small">
              Email gelmedi mi? Spam klasorunu kontrol edin veya birkacc dakika bekleyip tekrar deneyin.
            </p>
            <Link to="/login" className="btn-primary">
              Giris Sayfasina Don
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
        <h1>Sifremi Unuttum</h1>
        <p className="info-text" style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
          Email adresinizi girin, size sifre sifirlama linki gonderelim.
        </p>

        {error && <div className="error-message">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email">Email Adresi</label>
            <input
              type="email"
              id="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="ornek@konyalisaat.com.tr"
              required
              disabled={isLoading}
            />
          </div>

          <button type="submit" disabled={isLoading}>
            {isLoading ? 'Gonderiliyor...' : 'Sifirlama Linki Gonder'}
          </button>
        </form>

        <div className="register-link">
          <Link to="/login">Giris sayfasina don</Link>
        </div>
      </div>
    </div>
  );
}
