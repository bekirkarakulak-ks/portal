import { useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../services/api';
import type { EmailLookupResponse } from '../services/api';
import logo from '../assets/logo.svg';

export const Register = () => {
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [emailChecking, setEmailChecking] = useState(false);
  const [emailStatus, setEmailStatus] = useState<'unchecked' | 'valid' | 'invalid'>('unchecked');
  const [success, setSuccess] = useState(false);
  const [successMessage, setSuccessMessage] = useState('');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });

    // Email degistiginde status'u sifirla
    if (name === 'email') {
      setEmailStatus('unchecked');
    }
  };

  // Email kontrol fonksiyonu
  const checkEmail = useCallback(async () => {
    if (!formData.email || !formData.email.includes('@')) return;

    setEmailChecking(true);
    setError('');

    try {
      const result: EmailLookupResponse = await authApi.checkEmail(formData.email);

      if (result.found) {
        setEmailStatus('valid');

        // Organizasyondan gelen ad/soyad varsa otomatik doldur
        if (result.firstName) {
          setFormData((prev) => ({
            ...prev,
            firstName: result.firstName || prev.firstName,
            lastName: result.lastName || prev.lastName,
          }));
        }
      } else {
        setEmailStatus('invalid');
        setError(
          'Bu email adresi ile kayıt olamazsınız. Sadece kurum email adresleri (@konyalisaat.com.tr) kabul edilmektedir.'
        );
      }
    } catch {
      setEmailStatus('invalid');
      setError('Email kontrolü sırasında bir hata oluştu.');
    } finally {
      setEmailChecking(false);
    }
  }, [formData.email]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // Email kontrolu yapilmamissa yap
    if (emailStatus === 'unchecked') {
      await checkEmail();
      return;
    }

    if (emailStatus === 'invalid') {
      setError('Geçersiz email adresi. Lütfen kurum email adresinizi kullanın.');
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      setError('Şifreler eşleşmiyor');
      return;
    }

    if (formData.password.length < 6) {
      setError('Şifre en az 6 karakter olmalıdır');
      return;
    }

    setLoading(true);

    try {
      const result = await authApi.register({
        username: formData.username,
        email: formData.email,
        password: formData.password,
        firstName: formData.firstName,
        lastName: formData.lastName,
      });

      if (result.success) {
        setSuccess(true);
        setSuccessMessage(result.message);
      } else {
        setError(result.message);
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || 'Kayıt oluşturulamadı.');
    } finally {
      setLoading(false);
    }
  };

  // Basarili kayit mesaji
  if (success) {
    // Email dogrulama gerekli mi kontrol et
    const requiresVerification = successMessage.includes('dogrulama') || successMessage.includes('Email');

    return (
      <div className="login-container">
        <div className="login-card">
          <img src={logo} alt="Logo" className="login-logo" />
          <div className="success-container">
            <div className="success-icon">&#x2713;</div>
            <h2>Kayıt Başarılı!</h2>
            <p className="success-message">{successMessage}</p>
            {requiresVerification ? (
              <>
                <p className="info-text">
                  Email adresinize bir doğrulama linki gönderdik. Hesabınızı aktif etmek için lütfen
                  email kutunuzu kontrol edin.
                </p>
                <p className="info-text small">
                  Email gelmediyse spam/junk klasörünü kontrol edin.
                </p>
              </>
            ) : (
              <p className="info-text">
                Artık giriş yapabilirsiniz.
              </p>
            )}
            <Link to="/login" className="btn-primary">
              Giriş Sayfasına Git
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
        <h1>Kayit Ol</h1>

        <form onSubmit={handleSubmit}>
          {/* Email alani ilk sirada */}
          <div className="form-group">
            <label htmlFor="email">E-posta *</label>
            <div className="input-with-button">
              <input
                type="email"
                id="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                onBlur={checkEmail}
                required
                placeholder="ornek@konyalisaat.com.tr"
                className={emailStatus === 'valid' ? 'valid' : emailStatus === 'invalid' ? 'invalid' : ''}
              />
              {emailChecking && <span className="input-spinner"></span>}
              {emailStatus === 'valid' && <span className="input-check">&#x2713;</span>}
            </div>
            {emailStatus === 'valid' && (
              <span className="field-hint success">Email adresi doğrulandı</span>
            )}
          </div>

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="firstName">Ad *</label>
              <input
                type="text"
                id="firstName"
                name="firstName"
                value={formData.firstName}
                onChange={handleChange}
                required
              />
            </div>
            <div className="form-group">
              <label htmlFor="lastName">Soyad *</label>
              <input
                type="text"
                id="lastName"
                name="lastName"
                value={formData.lastName}
                onChange={handleChange}
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="username">Kullanıcı Adı *</label>
            <input
              type="text"
              id="username"
              name="username"
              value={formData.username}
              onChange={handleChange}
              required
              placeholder="Sisteme giriş için kullanacağınız ad"
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Şifre *</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required
              minLength={6}
              placeholder="En az 6 karakter"
            />
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword">Şifre Tekrar *</label>
            <input
              type="password"
              id="confirmPassword"
              name="confirmPassword"
              value={formData.confirmPassword}
              onChange={handleChange}
              required
            />
          </div>

          {error && <div className="error-message">{error}</div>}

          <button type="submit" disabled={loading || emailChecking}>
            {loading ? 'Kayıt Oluşturuluyor...' : emailStatus === 'unchecked' ? 'Email Kontrol Et' : 'Kayıt Ol'}
          </button>
        </form>

        <p className="register-link">
          Zaten hesabınız var mı? <Link to="/login">Giriş Yap</Link>
        </p>
      </div>
    </div>
  );
};

export default Register;
