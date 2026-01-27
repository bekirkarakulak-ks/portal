import { useEffect, useState } from 'react';
import { adminService } from '../../services/adminService';
import type { RoleResponse, PermissionResponse } from '../../services/adminService';

export default function RoleManagement() {
  const [roles, setRoles] = useState<RoleResponse[]>([]);
  const [permissions, setPermissions] = useState<PermissionResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleResponse | null>(null);
  const [formData, setFormData] = useState({
    code: '',
    name: '',
    description: '',
    permissionIds: [] as number[]
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [rolesRes, permsRes] = await Promise.all([
        adminService.getRoles(),
        adminService.getPermissions()
      ]);
      setRoles(rolesRes.data);
      setPermissions(permsRes.data);
    } catch (err) {
      setError('Veriler yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingRole(null);
    setFormData({ code: '', name: '', description: '', permissionIds: [] });
    setShowModal(true);
  };

  const handleEdit = (role: RoleResponse) => {
    setEditingRole(role);
    setFormData({
      code: role.code,
      name: role.name,
      description: role.description || '',
      permissionIds: role.permissionIds || []
    });
    setShowModal(true);
  };

  const handleSave = async () => {
    try {
      if (editingRole) {
        await adminService.updateRole(editingRole.id, {
          ...formData,
          isActive: true
        });
      } else {
        await adminService.createRole(formData);
      }
      setShowModal(false);
      fetchData();
    } catch (err) {
      setError('Kaydetme başarısız');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Bu rolü silmek istediğinizden emin misiniz?')) return;
    try {
      await adminService.deleteRole(id);
      fetchData();
    } catch (err) {
      setError('Silme başarısız');
    }
  };

  const togglePermission = (permId: number) => {
    setFormData(prev => ({
      ...prev,
      permissionIds: prev.permissionIds.includes(permId)
        ? prev.permissionIds.filter(id => id !== permId)
        : [...prev.permissionIds, permId]
    }));
  };

  // Group permissions by module
  const groupedPermissions = permissions.reduce((acc, perm) => {
    const key = perm.moduleName || 'Diğer';
    if (!acc[key]) acc[key] = [];
    acc[key].push(perm);
    return acc;
  }, {} as Record<string, PermissionResponse[]>);

  if (loading) return <div className="loading">Yükleniyor...</div>;

  return (
    <div className="admin-page">
      <div className="admin-header">
        <h2>Rol Yönetimi</h2>
        <button className="btn-primary" onClick={handleCreate}>
          + Yeni Rol
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      <table className="admin-table">
        <thead>
          <tr>
            <th>Kod</th>
            <th>Ad</th>
            <th>Açıklama</th>
            <th>Yetki Sayısı</th>
            <th>Durum</th>
            <th>İşlemler</th>
          </tr>
        </thead>
        <tbody>
          {roles.map(role => (
            <tr key={role.id}>
              <td><code>{role.code}</code></td>
              <td><strong>{role.name}</strong></td>
              <td>{role.description || '-'}</td>
              <td>{role.permissionIds?.length || 0}</td>
              <td>
                <span className={`status ${role.isActive ? 'approved' : 'rejected'}`}>
                  {role.isActive ? 'Aktif' : 'Pasif'}
                </span>
              </td>
              <td>
                <div className="table-actions">
                  <button className="btn-small btn-primary" onClick={() => handleEdit(role)}>
                    Düzenle
                  </button>
                  <button className="btn-small btn-danger" onClick={() => handleDelete(role.id)}>
                    Sil
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Role Modal */}
      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal modal-large" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{editingRole ? 'Rol Düzenle' : 'Yeni Rol'}</h3>
              <button className="modal-close" onClick={() => setShowModal(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label>Kod</label>
                <input
                  type="text"
                  value={formData.code}
                  onChange={e => setFormData(prev => ({ ...prev, code: e.target.value }))}
                  placeholder="ROLE_CODE"
                />
              </div>
              <div className="form-group">
                <label>Ad</label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={e => setFormData(prev => ({ ...prev, name: e.target.value }))}
                  placeholder="Rol Adi"
                />
              </div>
              <div className="form-group">
                <label>Açıklama</label>
                <textarea
                  value={formData.description}
                  onChange={e => setFormData(prev => ({ ...prev, description: e.target.value }))}
                  placeholder="Rol açıklaması..."
                />
              </div>

              <div className="form-group">
                <label>Yetkiler</label>
                <div className="permissions-tree">
                  {Object.entries(groupedPermissions).map(([moduleName, perms]) => (
                    <div key={moduleName} className="permission-module">
                      <h4>{moduleName}</h4>
                      <div className="permission-list">
                        {perms.map(perm => (
                          <label key={perm.id} className="permission-checkbox">
                            <input
                              type="checkbox"
                              checked={formData.permissionIds.includes(perm.id)}
                              onChange={() => togglePermission(perm.id)}
                            />
                            <span>
                              <strong>{perm.name}</strong>
                              <small>{perm.code}</small>
                            </span>
                          </label>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn-secondary" onClick={() => setShowModal(false)}>
                İptal
              </button>
              <button className="btn-primary" onClick={handleSave}>
                Kaydet
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
