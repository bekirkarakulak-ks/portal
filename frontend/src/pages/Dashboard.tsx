import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export const Dashboard = () => {
  const { user, hasPermission, logout } = useAuth();

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <h1>Portal</h1>
        <div className="user-info">
          <span>
            {user?.firstName} {user?.lastName}
          </span>
          <button onClick={logout} className="logout-btn">
            Cikis Yap
          </button>
        </div>
      </header>

      <main className="dashboard-content">
        <h2>Hosgeldiniz, {user?.firstName}!</h2>

        <div className="modules-grid">
          {/* IK Modulu */}
          {hasPermission('IK.Bordro.KendiGoruntule') && (
            <div className="module-card">
              <h3>Insan Kaynaklari</h3>
              <ul>
                {hasPermission('IK.Bordro.KendiGoruntule') && (
                  <li>
                    <Link to="/ik/bordro">Bordrom</Link>
                  </li>
                )}
                {hasPermission('IK.Izin.KendiGoruntule') && (
                  <li>
                    <Link to="/ik/izinler">Izinlerim</Link>
                  </li>
                )}
                {hasPermission('IK.Bordro.TumGoruntule') && (
                  <li>
                    <Link to="/ik/yonetim">IK Yonetimi</Link>
                  </li>
                )}
              </ul>
            </div>
          )}

          {/* Butce Modulu */}
          {hasPermission('BUTCE.Kendi.Goruntule') && (
            <div className="module-card">
              <h3>Butce</h3>
              <ul>
                {hasPermission('BUTCE.Kendi.Goruntule') && (
                  <li>
                    <Link to="/butce">Butcem</Link>
                  </li>
                )}
                {hasPermission('BUTCE.Tum.Goruntule') && (
                  <li>
                    <Link to="/butce/yonetim">Butce Yonetimi</Link>
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
    </div>
  );
};

export default Dashboard;
