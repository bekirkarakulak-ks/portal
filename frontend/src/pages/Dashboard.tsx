import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useAuthStore } from '../store/authStore';
import authService from '../services/authService';
import logo from '../assets/logo.svg';

export const Dashboard = () => {
  const { user, hasPermission, hasAnyPermission, logout } = useAuth();
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordError, setPasswordError] = useState('');
  const [passwordSuccess, setPasswordSuccess] = useState('');
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [activeApp, setActiveApp] = useState<string | null>(null);
  const [appUrl, setAppUrl] = useState<string>('');

  const openEmbeddedApp = (appName: string, baseUrl: string) => {
    const { accessToken, refreshToken } = useAuthStore.getState();
    const token = encodeURIComponent(accessToken || '');
    const refresh = encodeURIComponent(refreshToken || '');
    setAppUrl(`${baseUrl}?token=${token}&refresh=${refresh}`);
    setActiveApp(appName);
  };

  const closeEmbeddedApp = () => {
    setActiveApp(null);
    setAppUrl('');
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError('');
    setPasswordSuccess('');

    if (newPassword.length < 6) {
      setPasswordError('Yeni şifre en az 6 karakter olmalıdır.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setPasswordError('Şifreler eşleşmiyor.');
      return;
    }

    setIsChangingPassword(true);

    try {
      const response = await authService.changePassword({
        currentPassword,
        newPassword,
      });

      if (response.success) {
        setPasswordSuccess(response.message);
        setCurrentPassword('');
        setNewPassword('');
        setConfirmPassword('');
        setTimeout(() => {
          setShowPasswordModal(false);
          setPasswordSuccess('');
        }, 2000);
      } else {
        setPasswordError(response.message);
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      setPasswordError(error.response?.data?.message || 'Bir hata oluştu.');
    } finally {
      setIsChangingPassword(false);
    }
  };

  const closePasswordModal = () => {
    setShowPasswordModal(false);
    setCurrentPassword('');
    setNewPassword('');
    setConfirmPassword('');
    setPasswordError('');
    setPasswordSuccess('');
  };

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1>
          <img src={logo} alt="Logo" className="header-logo" />
          Portal
        </h1>
        <div className="user-info">
          <span>
            {user?.firstName} {user?.lastName}
          </span>
          <button onClick={() => setShowPasswordModal(true)} className="logout-btn">
            Şifre Değiştir
          </button>
          <button onClick={logout} className="logout-btn">
            Çıkış Yap
          </button>
        </div>
      </header>

      {/* Embedded App View */}
      {activeApp && (
        <div className="embedded-app-container">
          <div className="embedded-app-header">
            <button onClick={closeEmbeddedApp} className="back-to-dashboard">
              ← Portal'a Dön
            </button>
            <span className="embedded-app-title">{activeApp}</span>
          </div>
          <iframe
            src={appUrl}
            className="embedded-app-iframe"
            title={activeApp}
            allow="clipboard-read; clipboard-write"
          />
        </div>
      )}

      {/* Dashboard Content */}
      {!activeApp && (
        <main className="dashboard-content">
          <h2>Hoş geldiniz, {user?.firstName}!</h2>

          <div className="modules-grid">
          {/* IK Modulu */}
          {hasAnyPermission(['IK.Bordro.Kendi', 'IK.Izin.Kendi']) && (
            <div className="module-card">
              <h3>İnsan Kaynakları</h3>
              <ul>
                {hasPermission('IK.Bordro.Kendi') && (
                  <li>
                    <Link to="/ik/bordro">Bordrom</Link>
                  </li>
                )}
                {hasPermission('IK.Izin.Kendi') && (
                  <li>
                    <Link to="/ik/izinler">İzinlerim</Link>
                  </li>
                )}
                {hasPermission('IK.Bordro.Tumu') && (
                  <li>
                    <Link to="/ik/yonetim">İK Yönetimi</Link>
                  </li>
                )}
              </ul>
            </div>
          )}

          {/* Bütçe Modülü */}
          {hasPermission('Butce.Kendi') && (
            <div className="module-card">
              <h3>Bütçe</h3>
              <ul>
                {hasPermission('Butce.Kendi') && (
                  <li>
                    <Link to="/butce">Bütçem</Link>
                  </li>
                )}
                {hasPermission('Butce.Tumu') && (
                  <li>
                    <Link to="/butce/yonetim">Bütçe Yönetimi</Link>
                  </li>
                )}
              </ul>
            </div>
          )}

          {/* Admin Modülü */}
          {hasPermission('Admin.Access') && (
            <div className="module-card">
              <h3>Yönetim</h3>
              <ul>
                <li>
                  <Link to="/admin">Admin Paneli</Link>
                </li>
                {hasPermission('Admin.Kullanici.Goruntule') && (
                  <li>
                    <Link to="/admin/users">Kullanıcı Yönetimi</Link>
                  </li>
                )}
                {hasPermission('Admin.Rol.Goruntule') && (
                  <li>
                    <Link to="/admin/roles">Rol Yönetimi</Link>
                  </li>
                )}
              </ul>
            </div>
          )}

          {/* ERP Modülü - Admin yetkisi olanlar için */}
          {(hasPermission('Admin.Access') || hasAnyPermission(['Siparis.Donem.Goruntule', 'Siparis.Talep.Goruntule', 'Siparis.Limit.Goruntule'])) && (
            <div className="module-card">
              <h3>ERP Uygulamaları</h3>
              <ul>
                {hasPermission('Admin.Access') && (
                  <li>
                    <button
                      type="button"
                      className="link-button"
                      onClick={() => openEmbeddedApp('Konyali ERP', 'https://erp-web-518226731997.europe-west2.run.app')}
                    >
                      Konyali ERP (Transfer Analizi)
                    </button>
                  </li>
                )}
                {hasAnyPermission(['Siparis.Donem.Goruntule', 'Siparis.Talep.Goruntule', 'Siparis.Limit.Goruntule']) && (
                  <li>
                    <button
                      type="button"
                      className="link-button"
                      onClick={() => openEmbeddedApp('Mağaza Sipariş', 'https://siparis-web-518226731997.europe-west2.run.app')}
                    >
                      Mağaza Sipariş Sistemi
                    </button>
                  </li>
                )}
              </ul>
            </div>
          )}
        </div>

        <div className="permissions-info">
            <h3>Yetkileriniz</h3>
            <ul>
              {user?.permissions.map((perm) => (
                <li key={perm}>{perm}</li>
              ))}
            </ul>
          </div>
        </main>
      )}

      {/* Şifre Değiştirme Modal */}
      {showPasswordModal && (
        <div className="modal-overlay" onClick={closePasswordModal}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Şifre Değiştir</h3>
              <button className="modal-close" onClick={closePasswordModal}>
                &times;
              </button>
            </div>
            <form onSubmit={handleChangePassword}>
              <div className="modal-body">
                {passwordError && <div className="error-message">{passwordError}</div>}
                {passwordSuccess && (
                  <div className="error-message" style={{ background: '#ECFDF5', color: '#065F46', borderLeftColor: '#10B981' }}>
                    {passwordSuccess}
                  </div>
                )}

                <div className="form-group">
                  <label htmlFor="currentPassword">Mevcut Şifre</label>
                  <input
                    type="password"
                    id="currentPassword"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    required
                    disabled={isChangingPassword}
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="newPassword">Yeni Şifre</label>
                  <input
                    type="password"
                    id="newPassword"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="En az 6 karakter"
                    required
                    minLength={6}
                    disabled={isChangingPassword}
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="confirmPassword">Yeni Şifre Tekrar</label>
                  <input
                    type="password"
                    id="confirmPassword"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                    minLength={6}
                    disabled={isChangingPassword}
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn-secondary" onClick={closePasswordModal} disabled={isChangingPassword}>
                  İptal
                </button>
                <button type="submit" className="btn-primary" disabled={isChangingPassword}>
                  {isChangingPassword ? 'Kaydediliyor...' : 'Şifreyi Değiştir'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Dashboard;
