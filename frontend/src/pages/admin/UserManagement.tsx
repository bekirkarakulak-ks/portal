import { useEffect, useState } from 'react';
import { adminService } from '../../services/adminService';
import type { UserListItem, RoleResponse } from '../../services/adminService';

export default function UserManagement() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [roles, setRoles] = useState<RoleResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedUser, setSelectedUser] = useState<UserListItem | null>(null);
  const [selectedRoles, setSelectedRoles] = useState<number[]>([]);
  const [showRoleModal, setShowRoleModal] = useState(false);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [usersRes, rolesRes] = await Promise.all([
        adminService.getUsers(),
        adminService.getRoles()
      ]);
      setUsers(usersRes.data);
      setRoles(rolesRes.data);
    } catch (err) {
      setError('Veriler yüklenemedi');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleEditRoles = async (user: UserListItem) => {
    try {
      const userDetail = await adminService.getUser(user.username, user.client);
      setSelectedUser(user);
      setSelectedRoles(userDetail.data.roles.map(r => r.id));
      setShowRoleModal(true);
    } catch (err) {
      setError('Kullanıcı bilgileri alınamadı');
    }
  };

  const handleSaveRoles = async () => {
    if (!selectedUser) return;
    try {
      await adminService.updateUserRoles(selectedUser.username, selectedRoles);
      setShowRoleModal(false);
      fetchData();
    } catch (err) {
      setError('Roller güncellenemedi');
    }
  };

  const handleToggleActive = async (user: UserListItem) => {
    try {
      await adminService.updateUser(user.username, user.client, {
        firstName: user.firstName,
        lastName: user.lastName,
        email: user.email,
        isActive: !user.isActive
      });
      fetchData();
    } catch (err) {
      setError('Durum güncellenemedi');
    }
  };

  const toggleRole = (roleId: number) => {
    setSelectedRoles(prev =>
      prev.includes(roleId)
        ? prev.filter(id => id !== roleId)
        : [...prev, roleId]
    );
  };

  if (loading) return <div className="loading">Yükleniyor...</div>;

  return (
    <div className="admin-page">
      <div className="admin-header">
        <h2>Kullanıcı Yönetimi</h2>
        <span className="badge">{users.length} kullanıcı</span>
      </div>

      {error && <div className="error-message">{error}</div>}

      <table className="admin-table">
        <thead>
          <tr>
            <th>Kullanıcı Adı</th>
            <th>Ad Soyad</th>
            <th>Email</th>
            <th>Roller</th>
            <th>Durum</th>
            <th>Son Giriş</th>
            <th>İşlemler</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={`${user.username}-${user.client}`}>
              <td><strong>{user.username}</strong></td>
              <td>{user.firstName} {user.lastName}</td>
              <td>{user.email}</td>
              <td>
                <div className="role-badges">
                  {user.roles.map(role => (
                    <span key={role} className="role-badge">{role}</span>
                  ))}
                </div>
              </td>
              <td>
                <span className={`status ${user.isActive ? 'approved' : 'rejected'}`}>
                  {user.isActive ? 'Aktif' : 'Pasif'}
                </span>
              </td>
              <td>
                {user.lastLoginAt
                  ? new Date(user.lastLoginAt).toLocaleDateString('tr-TR')
                  : '-'}
              </td>
              <td>
                <div className="table-actions">
                  <button
                    className="btn-small btn-primary"
                    onClick={() => handleEditRoles(user)}
                  >
                    Roller
                  </button>
                  <button
                    className={`btn-small ${user.isActive ? 'btn-warning' : 'btn-success'}`}
                    onClick={() => handleToggleActive(user)}
                  >
                    {user.isActive ? 'Pasif Yap' : 'Aktif Yap'}
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Role Edit Modal */}
      {showRoleModal && selectedUser && (
        <div className="modal-overlay" onClick={() => setShowRoleModal(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Rol Atama - {selectedUser.username}</h3>
              <button className="modal-close" onClick={() => setShowRoleModal(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="role-list">
                {roles.map(role => (
                  <label key={role.id} className="role-checkbox">
                    <input
                      type="checkbox"
                      checked={selectedRoles.includes(role.id)}
                      onChange={() => toggleRole(role.id)}
                    />
                    <span className="role-info">
                      <strong>{role.name}</strong>
                      <small>{role.code}</small>
                      {role.description && <p>{role.description}</p>}
                    </span>
                  </label>
                ))}
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn-secondary" onClick={() => setShowRoleModal(false)}>
                İptal
              </button>
              <button className="btn-primary" onClick={handleSaveRoles}>
                Kaydet
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
