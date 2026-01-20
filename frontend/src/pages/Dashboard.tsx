import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
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

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError('');
    setPasswordSuccess('');

    if (newPassword.length < 6) {
      setPasswordError('Yeni sifre en az 6 karakter olmalidir.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setPasswordError('Sifreler eslesmiyor.');
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
      setPasswordError(error.response?.data?.message || 'Bir hata olustu.');
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
            Sifre Degistir
          </button>
          <button onClick={logout} className="logout-btn">
            Cikis Yap
          </button>
        </div>
      </header>

      <main className="dashboard-content">
        <h2>Hos geldiniz, {user?.firstName}!</h2>

        <div className="modules-grid">
          {/* IK Modulu */}
          {hasAnyPermission(['IK.Bordro.Kendi', 'IK.Izin.Kendi']) && (
            <div className="module-card">
              <h3>Insan Kaynaklari</h3>
              <ul>
                {hasPermission('IK.Bordro.Kendi') && (
                  <li>
                    <Link to="/ik/bordro">Bordrom</Link>
                  </li>
                )}
                {hasPermission('IK.Izin.Kendi') && (
                  <li>
                    <Link to="/ik/izinler">Izinlerim</Link>
                  </li>
                )}
                {hasPermission('IK.Bordro.Tumu') && (
                  <li>
                    <Link to="/ik/yonetim">IK Yonetimi</Link>
                  </li>
                )}
              </ul>
            </div>
          )}

          {/* Butce Modulu */}
          {hasPermission('Butce.Kendi') && (
            <div className="module-card">
              <h3>Butce</h3>
              <ul>
                {hasPermission('Butce.Kendi') && (
                  <li>
                    <Link to="/butce">Butcem</Link>
                  </li>
                )}
                {hasPermission('Butce.Tumu') && (
                  <li>
                    <Link to="/butce/yonetim">Butce Yonetimi</Link>
                  </li>
                )}
              </ul>
            </div>
          )}

          {/* Admin Modulu */}
          {hasPermission('Admin.Access') && (
            <div className="module-card">
              <h3>Yonetim</h3>
              <ul>
                <li>
                  <Link to="/admin">Admin Paneli</Link>
                </li>
                {hasPermission('Admin.Kullanici.Goruntule') && (
                  <li>
                    <Link to="/admin/users">Kullanici Yonetimi</Link>
                  </li>
                )}
                {hasPermission('Admin.Rol.Goruntule') && (
                  <li>
                    <Link to="/admin/roles">Rol Yonetimi</Link>
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

      {/* Şifre Değiştirme Modal */}
      {showPasswordModal && (
        <div className="modal-overlay" onClick={closePasswordModal}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Sifre Degistir</h3>
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
                  <label htmlFor="currentPassword">Mevcut Sifre</label>
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
                  <label htmlFor="newPassword">Yeni Sifre</label>
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
                  <label htmlFor="confirmPassword">Yeni Sifre Tekrar</label>
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
                  Iptal
                </button>
                <button type="submit" className="btn-primary" disabled={isChangingPassword}>
                  {isChangingPassword ? 'Kaydediliyor...' : 'Sifreyi Degistir'}
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
