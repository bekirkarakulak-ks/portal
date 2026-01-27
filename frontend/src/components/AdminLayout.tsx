import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export default function AdminLayout() {
  const { user, logout, hasPermission } = useAuth();

  const navItems = [
    { to: '/admin', label: 'Dashboard', icon: '📊', exact: true },
    { to: '/admin/users', label: 'Kullanıcılar', icon: '👥', permission: 'Admin.Kullanici.Goruntule' },
    { to: '/admin/roles', label: 'Roller', icon: '🔐', permission: 'Admin.Rol.Goruntule' },
    { to: '/admin/modules', label: 'Modüller', icon: '📦', permission: 'Admin.Modul.Goruntule' },
    { to: '/admin/organization', label: 'Organizasyon', icon: '🏢', permission: 'Admin.Org.Goruntule' },
  ];

  return (
    <div className="admin-layout">
      <header className="admin-topbar">
        <div className="topbar-left">
          <NavLink to="/" className="back-link">← Ana Sayfa</NavLink>
          <h1>Yönetim Paneli</h1>
        </div>
        <div className="topbar-right">
          <span className="user-info">
            {user?.firstName} {user?.lastName}
          </span>
          <button onClick={logout} className="logout-btn">Çıkış</button>
        </div>
      </header>

      <div className="admin-container">
        <aside className="admin-sidebar">
          <nav className="admin-nav">
            {navItems.map(item => {
              if (item.permission && !hasPermission(item.permission)) {
                return null;
              }
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.exact}
                  className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
                >
                  <span className="nav-icon">{item.icon}</span>
                  <span className="nav-label">{item.label}</span>
                </NavLink>
              );
            })}
          </nav>
        </aside>

        <main className="admin-main">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
