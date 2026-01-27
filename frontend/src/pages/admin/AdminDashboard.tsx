import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { adminService } from '../../services/adminService';
import type { AdminDashboard as DashboardData } from '../../services/adminService';

export default function AdminDashboard() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        const response = await adminService.getDashboard();
        setData(response.data);
      } catch (err) {
        setError('Dashboard verileri yüklenemedi');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchDashboard();
  }, []);

  if (loading) return <div className="loading">Yükleniyor...</div>;
  if (error) return <div className="error-message">{error}</div>;
  if (!data) return null;

  return (
    <div className="admin-dashboard">
      <h2>Yönetim Paneli</h2>

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-value">{data.totalUsers}</div>
          <div className="stat-label">Toplam Kullanıcı</div>
          <div className="stat-sub">{data.activeUsers} aktif</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{data.totalRoles}</div>
          <div className="stat-label">Rol</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{data.totalModules}</div>
          <div className="stat-label">Modül</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{data.totalPermissions}</div>
          <div className="stat-label">Yetki</div>
        </div>
        <div className="stat-card">
          <div className="stat-value">{data.organizationRules}</div>
          <div className="stat-label">Org. Kuralı</div>
        </div>
      </div>

      <div className="admin-sections">
        <div className="admin-section">
          <h3>Hızlı Erişim</h3>
          <div className="quick-links">
            <Link to="/admin/users" className="quick-link">
              <span className="icon">👥</span>
              <span>Kullanıcı Yönetimi</span>
            </Link>
            <Link to="/admin/roles" className="quick-link">
              <span className="icon">🔐</span>
              <span>Rol Yönetimi</span>
            </Link>
            <Link to="/admin/modules" className="quick-link">
              <span className="icon">📦</span>
              <span>Modül Yönetimi</span>
            </Link>
            <Link to="/admin/organization" className="quick-link">
              <span className="icon">🏢</span>
              <span>Organizasyon</span>
            </Link>
          </div>
        </div>

        <div className="admin-section">
          <h3>Son Kayıt Olan Kullanıcılar</h3>
          {data.recentUsers.length > 0 ? (
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Kullanıcı Adı</th>
                  <th>Ad Soyad</th>
                  <th>Email</th>
                  <th>Durum</th>
                </tr>
              </thead>
              <tbody>
                {data.recentUsers.map((user) => (
                  <tr key={user.username}>
                    <td>{user.username}</td>
                    <td>{user.firstName} {user.lastName}</td>
                    <td>{user.email}</td>
                    <td>
                      <span className={`status ${user.isActive ? 'approved' : 'rejected'}`}>
                        {user.isActive ? 'Aktif' : 'Pasif'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p>Henüz kullanıcı yok</p>
          )}
        </div>
      </div>
    </div>
  );
}
